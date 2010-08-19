using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.EventAdapters;
using WordLight.Extensions;
using WordLight.Search;

namespace WordLight
{
    public class TextViewWindow : NativeWindow, IDisposable
    {
        #region WinProc Messages

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x101;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_RBUTTONUP = 0x205;
        private const int WM_MBUTTONUP = 0x208;
        private const int WM_XBUTTONUP = 0x20C;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_XBUTTONDOWN = 0x20B;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_MBUTTONDBLCLK = 0x0209;
        private const int WM_RBUTTONDBLCLK = 0x0206;
        private const int WM_XBUTTONDBLCLK = 0x020D;
        private const int WM_PARENTNOTIFY = 0x0210;

        private const int WM_PAINT = 0x000F;
        private const int WM_ERASEBKGND = 0x0014;

        #endregion

        #region External methods

        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr rect, bool erase);

        #endregion
        
        private IVsTextView _view;
        private IVsTextLines _buffer;
        private IVsHiddenTextManager _hiddenTextManager;
        private string _previousSelectedText;
        private List<TextSpan> _searchMarks = new List<TextSpan>();
        private object _searchMarksSyncLock = new object(); 

        private int _lineHeight;

        private string _selectedText;
        private TextViewEventAdapter _viewEvents;

        private int topTextLineInView = 0;
        private int bottomTextLineInView = 0;

        private TextSearch _search;

        public TextViewWindow(IVsTextView view, IVsHiddenTextManager hiddenTextManager)
        {
            if (view == null) throw new ArgumentNullException("view");
            if (hiddenTextManager == null) throw new ArgumentNullException("hiddenTextManager");
            
            _view = view;
            _hiddenTextManager = hiddenTextManager;

            _lineHeight = _view.GetLineHeight();
            _buffer = view.GetBuffer();

            _search = new TextSearch(_buffer);
            _search.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(searcher_SearchCompleted);            

            _viewEvents = new TextViewEventAdapter(_view);
            _viewEvents.ScrollChanged += new EventHandler<ViewScrollChangedEventArgs>(ScrollChangedHandler);

            AssignHandle(view.GetWindowHandle());
        }

        public void Dispose()
        {
            _viewEvents.ScrollChanged -= ScrollChangedHandler;

            _viewEvents.Dispose();
            ReleaseHandle();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            switch (m.Msg)
            {
                case WM_KEYUP:
                case WM_KEYDOWN:
                case WM_LBUTTONUP:
                case WM_RBUTTONUP:
                case WM_MBUTTONUP:
                case WM_XBUTTONUP:
                case WM_LBUTTONDOWN:
                case WM_MBUTTONDOWN:
                case WM_RBUTTONDOWN:
                case WM_XBUTTONDOWN:
                case WM_LBUTTONDBLCLK:
                case WM_MBUTTONDBLCLK:
                case WM_RBUTTONDBLCLK:
                case WM_XBUTTONDBLCLK:
                    HandleUserInput();
                    break;

                case WM_PAINT:
                    Paint();
                    break;
            }
        }

        private void HandleUserInput()
        {
            string text = _view.GetSelectedText();

            if (text != _previousSelectedText)
            {
                _previousSelectedText = text;
                SelectionChanged(text);
            }
        }

        private void Paint()
        {
            using (Graphics g = Graphics.FromHwnd(this.Handle))
            {
                DrawSearchMarks(g);
            }
        }

        private void RepaintWindow()
        {
            InvalidateRect(Handle, IntPtr.Zero, true);
            UpdateWindow(Handle);
        }

        private void UpdateVisibleMarks()
        {
            RectangleF maxClipBounds = new RectangleF(0, 0, float.MaxValue, float.MaxValue);
            List<Rectangle> rectList;
            lock (_searchMarksSyncLock)
            {
                rectList = GetRectanglesForVisibleMarks(_searchMarks, maxClipBounds);
            }
            if (rectList.Count > 0)
            {
                foreach (var rect in rectList)
                {
                    IntPtr pRect = Marshal.AllocHGlobal(Marshal.SizeOf(rect));
                    try
                    {
                        Marshal.StructureToPtr(rect, pRect, false);
                        InvalidateRect(Handle, pRect, false);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pRect);
                    }
                }
            }
            UpdateWindow(Handle);
        }

        private void DrawSearchMarks(Graphics g)
        {
            List<Rectangle> rectList;
            lock (_searchMarksSyncLock)
            {
                rectList = GetRectanglesForVisibleMarks(_searchMarks, g.VisibleClipBounds);
            }
            if (rectList.Count > 0)
            {
                Pen pen = new Pen(AddinSettings.Instance.SearchMarkOutlineColor);
                g.DrawRectangles(pen, rectList.ToArray());
            }
        }

        private List<Rectangle> GetRectanglesForVisibleMarks(IList<TextSpan> marks, RectangleF visibleClipBounds)
        {
            List<Rectangle> rectList = new List<Rectangle>(marks.Count);

                foreach (TextSpan mark in marks)
                {
                    if (topTextLineInView <= mark.iEndLine && mark.iStartLine <= bottomTextLineInView)
                    {
                        Rectangle rect = GetVisibleRectangle(mark, _view, visibleClipBounds, _lineHeight);
                        if (rect != Rectangle.Empty)
                            rectList.Add((Rectangle)rect);
                    }
                }

            return rectList;
        }

        private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
        {
            if (e.ScrollInfo.IsVertical)
            {
                IVsLayeredTextView viewLayer = _view as IVsLayeredTextView;
                IVsTextLayer topLayer = null;
                IVsTextLayer bufferLayer = _buffer as IVsTextLayer;
                
                if (viewLayer != null)
                {
                    viewLayer.GetTopmostLayer(out topLayer);                    
                }

                if (topLayer != null && bufferLayer != null)
                {
                    int temp;
                    topLayer.LocalLineIndexToDeeperLayer(bufferLayer, e.ScrollInfo.firstVisibleUnit, 0, out topTextLineInView, out temp);
                    topLayer.LocalLineIndexToDeeperLayer(bufferLayer, e.ScrollInfo.firstVisibleUnit + e.ScrollInfo.visibleUnits, 0, out bottomTextLineInView, out temp);
                    bottomTextLineInView++;
                }
                else
                {
                    TextSpan entireSpan = _buffer.CreateSpanForAllLines();
                    topTextLineInView = entireSpan.iStartLine;
                    bottomTextLineInView = entireSpan.iEndLine;
                }
            }
        }

        private void SelectionChanged(string text)
        {
            _selectedText = text;
            lock (_searchMarksSyncLock)
            {
                _searchMarks.Clear();
            }

            if (!string.IsNullOrEmpty(text))
            {
                SearchWords();
            }
            RepaintWindow();
        }

        private void SearchWords()
        {
            TextSpan viewRange = _buffer.CreateSpanForAllLines();
            viewRange.iStartLine = topTextLineInView;
			if (bottomTextLineInView < viewRange.iEndLine)
            {
                viewRange.iEndLine = bottomTextLineInView;
                viewRange.iEndIndex = 0;
            }

            var marks = _search.SearchOccurrences(_selectedText, viewRange);
            lock (_searchMarksSyncLock)
            {
                _searchMarks.AddRange(marks);
            }
            _search.SearchOccurrencesDelayed(_selectedText, _buffer.CreateSpanForAllLines());
        }

        private void searcher_SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            if (e.Text == _selectedText)
            {
                lock (_searchMarksSyncLock)
                {
                    _searchMarks.Clear();
                    _searchMarks.AddRange(e.Marks);
                }
                UpdateVisibleMarks();
            }
        }

        private Rectangle GetVisibleRectangle(TextSpan span, IVsTextView view, RectangleF visibleClipBounds, int lineHeight)
        {
            Rectangle rect = Rectangle.Empty;

            Point startPoint = view.GetPointOfLineColumn(span.iStartLine, span.iStartIndex);
            if (startPoint == Point.Empty)
                return rect;

            Point endPoint = view.GetPointOfLineColumn(span.iEndLine, span.iEndIndex);
            if (endPoint == Point.Empty)
                return rect;

            bool isVisible =
                visibleClipBounds.Left <= endPoint.X && startPoint.X <= visibleClipBounds.Right
                && visibleClipBounds.Top <= endPoint.Y && startPoint.Y <= visibleClipBounds.Bottom;

            if (isVisible)
            {
                int height = endPoint.Y - startPoint.Y + lineHeight;
                int width = endPoint.X - startPoint.X;

                int x = startPoint.X;
                int y = startPoint.Y;

                rect = new Rectangle(x, y, width, height);
            }

            return rect;
        }
    }
}
