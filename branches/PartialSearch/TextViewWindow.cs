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

        #endregion

        #region External methods

        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr rect, bool erase);

        #endregion

        private string _previousSelectedText;
        private IList<SearchMark> _cachedSearchMarks = null;
        private IVsTextView _view;
        private IVsHiddenTextManager _hiddenTextManager;

        //TODO: Move to SearchMark
        private int _lineHeight;

        private TextLineCache _textLines;

        private string _selectedText;
        private TextViewEventAdapter viewEvents;

        public TextViewWindow(IVsTextView view, IVsHiddenTextManager hiddenTextManager)
        {
            if (view == null) throw new ArgumentNullException("view");
            _view = view;

            _hiddenTextManager = hiddenTextManager;

            _textLines = new TextLineCache(view, _hiddenTextManager);

            _lineHeight = _view.GetLineHeight();

            viewEvents = new TextViewEventAdapter(_view);
            viewEvents.ScrollChanged += new EventHandler<ViewScrollChangedEventArgs>(ScrollChangedHandler);

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
            if (_cachedSearchMarks.IsNotNullAndEmpty())
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

        private void DrawSearchMarks(Graphics g, IList<SearchMark> markList)
        {
            if (markList == null) throw new ArgumentNullException("markList");

            var rectList = new List<Rectangle>(markList.Count);

            foreach (SearchMark mark in markList)
            {
                Rectangle rect = mark.GetVisibleRectangle(_view, g.VisibleClipBounds, _lineHeight);
                if (rect != Rectangle.Empty)
                    rectList.Add((Rectangle)rect);
            }

            if (rectList.Count > 0)
            {
                Pen pen = new Pen(AddinSettings.Instance.SearchMarkOutlineColor);
                g.DrawRectangles(pen, rectList.ToArray());
            }
        }

        private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
        {
            _cachedSearchMarks = SearchWords(_selectedText);
        }

        private void SelectionChanged(string text)
        {
            _selectedText = text;

            IList<SearchMark> previousSearch = _cachedSearchMarks;
            
            if (!string.IsNullOrEmpty(text))
            {
                _textLines = new TextLineCache(_view, _hiddenTextManager);
            }

            _cachedSearchMarks = SearchWords(_selectedText);

            if (_cachedSearchMarks.IsNotNullAndEmpty() || !previousSearch.IsNotNullAndEmpty())
            {
                Refresh();
            }
        }

        private IList<SearchMark> SearchWords(string text)
        {
            IList<SearchMark> marks = null;

            if (!string.IsNullOrEmpty(text))
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    marks = SearchWordsInViewPort(text, g.VisibleClipBounds);
                };
            }

            return marks;
        }

        private IList<SearchMark> SearchWordsInViewPort(string text, RectangleF clipBounds)
        {
            IVsTextLines buffer = _view.GetBuffer();

            TextSpan entireSpan = buffer.CreateSpanForAllLines();

            TextSpan searchRange = new TextSpan();

            searchRange.iStartLine = _textLines.GetLineByScreenY(_view, (int)clipBounds.Top);
            searchRange.iEndLine = _textLines.GetLineByScreenY(_view, (int)clipBounds.Bottom);

            if (searchRange.iEndLine == entireSpan.iEndLine)
                searchRange.iEndIndex = entireSpan.iEndIndex;
            else
                searchRange.iEndLine++;

            return buffer.SearchWords(text, searchRange);
        }
    }
}
