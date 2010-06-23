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
	public class TextPaneWindow : NativeWindow
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

		//private struct ScrollInfo
		//{
		//    public int MinUnit;
		//    public int MaxUnit;
		//    public int VisibleUnit;
		//    public int FirstVisibleUnit;
		//}

		//private enum ScrollBarTypes : int
		//{
		//    Horizontal = 0,
		//    Vertical = 1
		//}

        private string _previousSelectedText;
		public event EventHandler OnInput;

		private List<WordMark> _marks = new List<WordMark>();

		//private ScrollInfo _horizontalScroll;
		//private ScrollInfo _verticalScroll;

        private IVsTextView _view;
        private TextPane _pane;
		
		//public int ScrollHoriz
		//{
		//    get { return _horizontalScroll.FirstVisibleUnit; }
		//}

		//public int ScrollVert
		//{
		//    get { return _verticalScroll.FirstVisibleUnit; }
		//}

        public TextPaneWindow(IntPtr paneHandle, TextPane pane, IVsTextView view)
		{
            _pane = pane;
            _view = view;
			AssignHandle(paneHandle);
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
			//_horizontalScroll = GetScrollInfo(ScrollBarTypes.Horizontal);
			//_verticalScroll = GetScrollInfo(ScrollBarTypes.Vertical);

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

		public void AddMark(EditPoint start, EditPoint end)
		{
			var mark = new WordMark();
			mark.StartLine = start.Line;
			mark.StartLineIndex = start.LineCharOffset;
			mark.EndLine = end.Line;
			mark.EndLineIndex = end.LineCharOffset;

			_marks.Add(mark);
		}

		public void ClearMarks()
		{
			_marks.Clear();
		}

		public void Refresh()
		{
			InvalidateRect(Handle, IntPtr.Zero, true);
			UpdateWindow(Handle);
		}

		[DllImport("user32.dll", SetLastError = false)]
		private static extern bool UpdateWindow(IntPtr hWnd);

		[DllImport("user32.dll", SetLastError = false)]
		private static extern bool InvalidateRect(IntPtr hWnd, IntPtr rect, bool erase);

		//private ScrollInfo GetScrollInfo(ScrollBarTypes iBar)
		//{
		//    ScrollInfo data = new ScrollInfo();
			
		//    if (View != null)
		//    {
		//        View.GetScrollInfo((int)iBar, out data.MinUnit, out data.MaxUnit, out data.VisibleUnit, out data.FirstVisibleUnit);
		//    }
			
		//    return data;
		//}

		private void HandleUserInput()
		{
			EventHandler evt = OnInput;
			if (evt != null) evt(this, EventArgs.Empty);

            //if (_applicationObject == null || _applicationObject.ActiveDocument == null)
            //    return;

            TextDocument textDoc = _pane.Window.Document.Object("TextDocument") as TextDocument;
            if (textDoc != null)
            {
                string selectedText = GetSelectedText(textDoc);

                if (selectedText != _previousSelectedText)
                {
                    _previousSelectedText = selectedText;
                    ProcessSelectedText(textDoc, selectedText);
                }
            }
		}

        private string GetSelectedText(TextDocument textDoc)
        {
            string text = string.Empty;

            TextSelection selection = textDoc.Selection as TextSelection;
            if (selection != null)
            {
                text = selection.Text;
            }

            if (text == null)
                text = string.Empty;
            else
                text = text.Trim();

            return text;
        }

        private void ProcessSelectedText(TextDocument textDoc, string text)
        {
            ClearMarks();

            if (!string.IsNullOrEmpty(text))
            {
                bool result;
                EditPoint searchStart = textDoc.StartPoint.CreateEditPoint();
                EditPoint searchEnd = textDoc.EndPoint.CreateEditPoint();
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

            Refresh();

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
	}
}
