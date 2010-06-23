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
		private IList<WordMark> _marks;
        private IVsTextView _view;

        public TextViewWindow(IVsTextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

			_view = view;
			_marks = new List<WordMark>();

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
			using (Graphics g = Graphics.FromHwnd(this.Handle))
			{
				System.Drawing.Drawing2D.GraphicsContainer cont = g.BeginContainer();

				// Draw marks
				foreach (WordMark mark in _marks)
				{
					mark.Draw(g, _view);
				}

				g.EndContainer(cont);
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

				object tempPointer;				
				
				buffer.CreateEditPoint(0, 0, out tempPointer);
				EditPoint searchStart = tempPointer as EditPoint;

				buffer.CreateEditPoint(lastLine, lastLineCol, out tempPointer);
				EditPoint searchEnd = tempPointer as EditPoint;

				if (searchStart != null && searchEnd != null)
				{
					bool result;
					TextRanges ranges = null;
					do
					{
						result = searchStart.FindPattern(text, (int)vsFindOptions.vsFindOptionsNone, ref searchEnd, ref ranges);
						if (result)
						{
							AddMark(searchStart, searchEnd);
						}
						searchStart = searchEnd;
					} while (result);
				}
            }

            Refresh();

			//TODO
			
			//int highlightID;
            //Guid highlightGuid = ...; // your highlighted text style guid
            //textManager.GetRegisteredMarkerTypeID(ref highlightGuid, out highlightID);

            //// highlighting text block in the active view
            //IVsTextView view;
            //int result = textManager.GetActiveView(0, null, out view);
            //IVsTextLines buffer;
            //view.GetBuffer(out buffer);
            //buffer.CreateLineMarker(highlightID, startLine, startColumn, endLine, endColumn, null, null);
        }

		private void AddMark(EditPoint start, EditPoint end)
		{
			var mark = new WordMark();
			mark.StartLine = start.Line;
			mark.StartLineIndex = start.LineCharOffset;
			mark.EndLine = end.Line;
			mark.EndLineIndex = end.LineCharOffset;

			_marks.Add(mark);
		}
	}
}
