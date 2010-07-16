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
            return text;
        }

        private void ProcessSelectedText(string text)
        {
			_marks.Clear();

            if (!string.IsNullOrEmpty(text))
            {
				IVsTextLines buffer;
				_view.GetBuffer(out buffer);

				int lastLine;
				int lastLineCol;
				buffer.GetLastLineIndex(out lastLine, out lastLineCol);

				EditPoint searchStart = CreateEditPoint(buffer, 0, 0);
				EditPoint searchEnd = CreateEditPoint(buffer, lastLine, lastLineCol);

				if (searchStart != null && searchEnd != null)
				{
					bool result;
					TextRanges ranges = null;
					do
					{
						result = searchStart.FindPattern(text, (int)vsFindOptions.vsFindOptionsNone, ref searchEnd, ref ranges);
						if (result)
						{
							_marks.Add(new SearchMark(_view, _lineHeight, searchStart, searchEnd));
						}
						searchStart = searchEnd;
					} while (result);
				}
            }

            Refresh();
        }

		private EditPoint CreateEditPoint(IVsTextLines buffer, int line, int lineCol)
		{
			object tempPointer;
			buffer.CreateEditPoint(line, lineCol, out tempPointer);
			return tempPointer as EditPoint;
		}
	}
}
