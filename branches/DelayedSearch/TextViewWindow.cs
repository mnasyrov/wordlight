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
using WordLight.Searchers;

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

        private string _previousSelectedText;
        private IList<TextSpan> _cachedSearchMarks = new List<TextSpan>();
        private IVsTextView _view;
        private IVsHiddenTextManager _hiddenTextManager;

        private int _lineHeight;

        private string _selectedText;
        private TextViewEventAdapter viewEvents;

        private int topTextLineInView = 0;
        private int bottomTextLineInView = 0;

        private ITextSearch textSearch;

        public TextViewWindow(IVsTextView view, IVsHiddenTextManager hiddenTextManager)
        {
            if (view == null) throw new ArgumentNullException("view");
            _view = view;

            _hiddenTextManager = hiddenTextManager;

            _lineHeight = _view.GetLineHeight();

            viewEvents = new TextViewEventAdapter(_view);
            viewEvents.ScrollChanged += new EventHandler<ViewScrollChangedEventArgs>(ScrollChangedHandler);

            textSearch = new TextSearchByTimer();
            //textSearch = new TextSearchByThreadWorker();
            textSearch.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(searcher_SearchCompleted);

            AssignHandle(view.GetWindowHandle());
        }

        public void Dispose()
        {
            viewEvents.ScrollChanged -= ScrollChangedHandler;

            viewEvents.Dispose();
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
            if (_cachedSearchMarks.Count > 0)
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    DrawSearchMarks(g, _cachedSearchMarks);
                }
            }
        }

        private void Refresh()
        {
            InvalidateRect(Handle, IntPtr.Zero, true);
            UpdateWindow(Handle);
        }

        private void DrawSearchMarks(Graphics g, IList<TextSpan> markList)
        {
            var rectList = new List<Rectangle>(markList.Count);

            foreach (TextSpan mark in markList)
            {
                if (topTextLineInView <= mark.iEndLine && mark.iStartLine <= bottomTextLineInView)
                {
                    Rectangle rect = GetVisibleRectangle(mark, _view, g.VisibleClipBounds, _lineHeight);
                    if (rect != Rectangle.Empty)
                        rectList.Add((Rectangle)rect);
                }
            }

            if (rectList.Count > 0)
            {
                Pen pen = new Pen(AddinSettings.Instance.SearchMarkOutlineColor);
                g.DrawRectangles(pen, rectList.ToArray());
            }
        }

        private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
        {
            if (e.ScrollInfo.IsVertical)
            {
                IVsTextLines buffer = _view.GetBuffer();
                IVsHiddenTextSession hiddenTextSession = _hiddenTextManager.GetHiddenTextSession(buffer);

                TextSpan entireSpan = buffer.CreateSpanForAllLines();
                IList<TextSpan> hiddenRegions = hiddenTextSession.GetAllHiddenRegions(entireSpan);

                int viewLine = -1;
                int textLine = -1;

                while (
                    textLine <= entireSpan.iEndLine &&
                    viewLine <= e.ScrollInfo.maxUnit &&
                    viewLine <= e.ScrollInfo.firstVisibleUnit + e.ScrollInfo.visibleUnits)
                {
                    textLine++;

                    if (!IsTextLineHidden(textLine, hiddenRegions))
                    {
                        viewLine++;
                        if (viewLine == e.ScrollInfo.firstVisibleUnit)
                            topTextLineInView = textLine;

                        if (viewLine == e.ScrollInfo.firstVisibleUnit + e.ScrollInfo.visibleUnits)
                            bottomTextLineInView = textLine;
                    }
                }
            }
        }

        private bool IsTextLineHidden(int line, IList<TextSpan> hiddenRegions)
        {
            foreach (var hiddenSpan in hiddenRegions)
            {
                if (hiddenSpan.iStartLine <= line && line <= hiddenSpan.iEndLine)
                    return true;
            }
            return false;
        }

        private void SelectionChanged(string text)
        {
            _selectedText = text;
            _cachedSearchMarks = new List<TextSpan>();

            if (string.IsNullOrEmpty(text))
            {
                Refresh();
            }
            else
            {
                SearchWords();
                Refresh();
            }
        }

        private void SearchWords()
        {
            IVsTextLines buffer = _view.GetBuffer();

            //TextSpan viewRange = buffer.CreateSpanForAllLines();
            //viewRange.iStartLine = topTextLineInView;
            //if (viewRange.iEndLine != bottomTextLineInView)
            //{
            //    viewRange.iEndLine = bottomTextLineInView;
            //    viewRange.iEndIndex = 0;
            //}

            //_cachedSearchMarks = buffer.SearchWords(_selectedText, viewRange);
            textSearch.SearchAsync(buffer, _selectedText, buffer.CreateSpanForAllLines());
        }

        private void searcher_SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            if (e.Text == _selectedText)
            {
                _cachedSearchMarks = e.Marks;
                Refresh();
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
