using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using WordLight.EventAdapters;

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

		private TextLineCollection _textLines;

		private string selectedText;
		private TextViewEventAdapter viewEvents;

		public TextViewWindow(IVsTextView view)
		{
			if (view == null) throw new ArgumentNullException("view");
			_view = view;

			_textLines = new TextLineCollection(view);

			_marks = new List<SearchMark>();

			_view.GetLineHeight(out _lineHeight);

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

		private void Paint()
		{
			if (_marks.Count == 0)
				return;

			using (Graphics g = Graphics.FromHwnd(this.Handle))
			{
				List<Rectangle> rectList = new List<Rectangle>();

				foreach (SearchMark mark in _marks)
				{
					Rectangle rect = mark.GetVisibleRectangle(g.VisibleClipBounds);
					if (rect != Rectangle.Empty)
						rectList.Add((Rectangle)rect);
				}

				if (rectList.Count > 0)
				{
					Pen pen = new Pen(AddinSettings.Instance.SearchMarkOutlineColor);
					g.DrawRectangles(pen, rectList.ToArray());
				}
			}
		}

		private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
		{
			if (!string.IsNullOrEmpty(selectedText))
			{
				using (Graphics g = Graphics.FromHwnd(this.Handle))
				{
					_marks = SearchWordsInViewPort(selectedText, g.VisibleClipBounds);
				};
			}
		}

		private void Refresh()
		{
			InvalidateRect(Handle, IntPtr.Zero, true);
			UpdateWindow(Handle);
		}

		private void HandleUserInput()
		{
			selectedText = GetSelectedText();

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
				_textLines = new TextLineCollection(_view);
				
				using (Graphics g = Graphics.FromHwnd(this.Handle))
				{
					_marks = SearchWordsInViewPort(selectedText, g.VisibleClipBounds);
				}
			}

			if (_marks.Count != 0 || previousMarkCount != 0)
			{
				Refresh();
			}
		}

		private EditPoint CreateEditPoint(IVsTextLines buffer, int line, int lineCol)
		{
			object tempPointer;
			buffer.CreateEditPoint(line, lineCol, out tempPointer);
			return tempPointer as EditPoint;
		}

		private List<SearchMark> SearchWordsInViewPort(string text, RectangleF clipBounds)
		{
			IVsTextLines buffer;
			_view.GetBuffer(out buffer);

			int lastLine;
			int lastLineCol;
			buffer.GetLastLineIndex(out lastLine, out lastLineCol);

			int startLine = _textLines.GetLineIndexByScreenY((int)clipBounds.Top, _view);
			int endLine = _textLines.GetLineIndexByScreenY((int)clipBounds.Bottom, _view);

			int endLineCol = 0;

			if (endLine == lastLine)
				endLineCol = lastLineCol;
			else
				endLine++;

			return SearchWords(text, startLine, 0, endLine, endLineCol);
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

		//private Point GetScreenPositionOfText(int line, int column)
		//{
		//    var p = new Microsoft.VisualStudio.OLE.Interop.POINT[1];
		//    _view.GetPointOfLineColumn(line, column, p);
		//    return new Point(p[0].x, p[0].y);
		//}

		private struct ScrollInfo : IEquatable<ScrollInfo>
		{
			public int minUnit;
			public int maxUnit;
			public int visibleUnits;
			public int firstVisibleUnit;

			public static ScrollInfo Empty = new ScrollInfo()
			{
				firstVisibleUnit = 0,
				maxUnit = 0,
				minUnit = 0,
				visibleUnits = 0
			};

			public static ScrollInfo CreateByView(IVsTextView view, int iBar)
			{
				ScrollInfo info = new ScrollInfo();
				view.GetScrollInfo(iBar, out info.minUnit, out info.maxUnit, out info.visibleUnits, out info.firstVisibleUnit);
				return info;
			}

			public bool Equals(ScrollInfo other)
			{
				return 
					minUnit == other.minUnit &&
					maxUnit == other.maxUnit &&
					visibleUnits == other.visibleUnits &&
					firstVisibleUnit == other.firstVisibleUnit;
			}
		}

		private ScrollInfo lastHoriz = ScrollInfo.Empty;
		private ScrollInfo lastVert = ScrollInfo.Empty;

		private bool IsScrollPositionChanged()
		{
			ScrollInfo horiz = ScrollInfo.CreateByView(_view, 0);
			ScrollInfo vert = ScrollInfo.CreateByView(_view, 1);

			bool isChanged = !lastHoriz.Equals(horiz) || !lastVert.Equals(vert);

			lastVert = vert;
			lastHoriz = horiz;

			return isChanged;
		}
	}
}
