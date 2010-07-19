using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

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
		private IList<SearchMark> _marks;
        private IVsTextView _view;
		private int _lineHeight;

        public TextViewWindow(IVsTextView view)
		{
			if (view == null) throw new ArgumentNullException("view");
			_view = view;

            _marks = new List<SearchMark>();

			_view.GetLineHeight(out _lineHeight);

			AssignHandle(view.GetWindowHandle());
		}

		public void Dispose()
		{
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

		private void Paint()
		{
            if (_marks.Count == 0)
                return;

            int minUnit;
            int maxUnit;
            int visibleUnits;
            int firstVisibleUnit;
            _view.GetScrollInfo(1, out minUnit, out maxUnit, out visibleUnits, out firstVisibleUnit);

            List<Rectangle> rectList = new List<Rectangle>();

            using (Graphics g = Graphics.FromHwnd(this.Handle))
			{
                foreach (SearchMark mark in _marks)
                {
                    if (mark.IsVisible(firstVisibleUnit, firstVisibleUnit + visibleUnits))
                    {
                        Rectangle rect = mark.GetVisibleRectangle(g.VisibleClipBounds);
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
		}
		
		private void Refresh()
		{
			InvalidateRect(Handle, IntPtr.Zero, true);
			UpdateWindow(Handle);
		}

		private void HandleUserInput()
		{
            string selectedText = GetSelectedText();
			
            if (selectedText != _previousSelectedText)
            {
                _previousSelectedText = selectedText;
                ProcessSelectedText(selectedText);
            }
		}

        private string GetSelectedText()
        {
            string text;
			_view.GetSelectedText(out text);
			
			if (!string.IsNullOrEmpty(text))
				text = text.Trim();
            
			return text;
        }

        private void ProcessSelectedText(string text)
        {
			int previousMarkCount = _marks.Count;

			_marks.Clear();

            if (!string.IsNullOrEmpty(text))
            {
				IVsTextLines buffer;
				_view.GetBuffer(out buffer);

				int lastLine;
				int lastLineCol;
				buffer.GetLastLineIndex(out lastLine, out lastLineCol);


                RectangleF clipBounds;
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    clipBounds = g.VisibleClipBounds;
                }

                int startLine = SearchVisibleLine(clipBounds.Top, 0, lastLine);
                //int endLine = SearchVisibleLine(clipBounds.Bottom, 0, lastLine);

                //int startLine = 0;
                int endLine = lastLine;

                int endLineCol = 0;

                if (endLine == lastLine)
                    endLineCol = lastLineCol;
                else
                    endLine++;

                _marks = SearchWords(text, startLine, 0, endLine, endLineCol);
            }

			if (_marks.Count != 0 || previousMarkCount != 0)
			{
				Refresh();
			}
        }

        private int SearchVisibleLine(float top, int startLine, int endLine)
        {
            if (startLine >= endLine) 
                return startLine;

            var p = new Microsoft.VisualStudio.OLE.Interop.POINT[1];
            int middle = startLine + (endLine - startLine) / 2;;

            do
            {
                _view.GetPointOfLineColumn(middle, 0, p);

                if (p[0].x == 0 && p[0].y == 0)
                    middle--;
            }
            while (middle > top);

            if (top <= p[0].y)
                return SearchVisibleLine(top, startLine, middle);
            else
                return SearchVisibleLine(top, middle, endLine);
        }

		private EditPoint CreateEditPoint(IVsTextLines buffer, int line, int lineCol)
		{
			object tempPointer;
			buffer.CreateEditPoint(line, lineCol, out tempPointer);
			return tempPointer as EditPoint;
		}

        private List<SearchMark> SearchWords(string text, int startLine, int startLineCol, int endLine, int endLineCol)
        {
            List<SearchMark> marks = new List<SearchMark>();

            IVsTextLines buffer;
            _view.GetBuffer(out buffer);

            EditPoint searchStart = CreateEditPoint(buffer, startLine, startLineCol);
            EditPoint searchEnd = CreateEditPoint(buffer, endLine, endLineCol);

            if (searchStart != null && searchEnd != null)
            {
                bool result;
                TextRanges ranges = null;
                EditPoint wordEnd = null;

                do
                {
                    result = searchStart.FindPattern(text, (int)vsFindOptions.vsFindOptionsNone, ref wordEnd, ref ranges);
                    if (result)
                    {
                        //Do not process multi-line selections
                        if (searchStart.Line != wordEnd.Line)
                            break;

                        marks.Add(new SearchMark(_view, _lineHeight, searchStart, wordEnd));
                    }
                    searchStart = wordEnd;
                } while (result && searchStart.LessThan(searchEnd));
            }

            return marks;
        }
	}
}
