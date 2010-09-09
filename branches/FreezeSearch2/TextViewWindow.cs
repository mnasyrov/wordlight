using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.DllImport;
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

        private TextView _textView;

		private IVsHiddenTextManager _hiddenTextManager;
		private string _previousSelectedText;

		private MarkCollection _searchMarks = new MarkCollection();
		private MarkCollection _freezeMarks1 = new MarkCollection();
		private MarkCollection _freezeMarks2 = new MarkCollection();
		private MarkCollection _freezeMarks3 = new MarkCollection();

		private string _selectedText;
		private TextViewEventAdapter _viewEvents;
		private TextStreamEventAdapter _textStreamEvents;

		private int leftTextColumnInView = 0;

		private TextSearch _search;
		private TextSearch _freezeSearch1;
		private TextSearch _freezeSearch2;
		private TextSearch _freezeSearch3;

		private string _freezeText1;
		private string _freezeText2;
		private string _freezeText3;

		private TextSpan _viewRange = new TextSpan();

		public event EventHandler GotFocus;
		public event EventHandler LostFocus;

		private int _leftMarginWidth = 0;

		private int _visibleTextStart;
		private int _visibleTextEnd;

		private UpdateRectangle _markUpdateRect;

		public TextViewWindow(IVsTextView view, IVsHiddenTextManager hiddenTextManager)
		{
			if (view == null) throw new ArgumentNullException("view");
			if (hiddenTextManager == null) throw new ArgumentNullException("hiddenTextManager");

            _textView = new TextView(view);

			_hiddenTextManager = hiddenTextManager;

            _textStreamEvents = new TextStreamEventAdapter(_textView.Buffer);
			_textStreamEvents.StreamTextChanged += new EventHandler<StreamTextChangedEventArgs>(StreamTextChangedHandler);

            _search = new TextSearch(_textView.Buffer);
			_search.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(searcher_SearchCompleted);

            _freezeSearch1 = new TextSearch(_textView.Buffer);
			_freezeSearch1.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted1);

            _freezeSearch2 = new TextSearch(_textView.Buffer);
			_freezeSearch2.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted2);

            _freezeSearch3 = new TextSearch(_textView.Buffer);
			_freezeSearch3.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted3);

            _viewEvents = new TextViewEventAdapter(_textView.View);
			_viewEvents.ScrollChanged += new EventHandler<ViewScrollChangedEventArgs>(ScrollChangedHandler);
			_viewEvents.GotFocus += new EventHandler<ViewFocusEventArgs>(GotFocusHandler);
			_viewEvents.LostFocus += new EventHandler<ViewFocusEventArgs>(LostFocusHandler);

			_searchMarks.MarkAdded += new EventHandler<MarkEventArgs>(MarkModifiedHandler);
			_searchMarks.MarkDeleted += new EventHandler<MarkEventArgs>(MarkDeletedHandler);
			_freezeMarks1.MarkAdded += new EventHandler<MarkEventArgs>(MarkModifiedHandler);
			_freezeMarks1.MarkDeleted += new EventHandler<MarkEventArgs>(MarkDeletedHandler);
			_freezeMarks2.MarkAdded += new EventHandler<MarkEventArgs>(MarkModifiedHandler);
			_freezeMarks2.MarkDeleted += new EventHandler<MarkEventArgs>(MarkDeletedHandler);
			_freezeMarks3.MarkAdded += new EventHandler<MarkEventArgs>(MarkModifiedHandler);
			_freezeMarks3.MarkDeleted += new EventHandler<MarkEventArgs>(MarkDeletedHandler);

			IntPtr hWnd = view.GetWindowHandle();
			_markUpdateRect = new UpdateRectangle(hWnd);

			AssignHandle(hWnd);
		}

		public void Dispose()
		{
			_textStreamEvents.StreamTextChanged -= StreamTextChangedHandler;
			_textStreamEvents.Dispose();

			_viewEvents.GotFocus -= GotFocusHandler;
			_viewEvents.LostFocus -= LostFocusHandler;
			_viewEvents.ScrollChanged -= ScrollChangedHandler;

			_viewEvents.Dispose();
			ReleaseHandle();
		}

		//Rectangle clipRect = Rectangle.Empty;

		protected override void WndProc(ref Message m)
		{
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
					base.WndProc(ref m);
					HandleUserInput();
					break;

				//case WM_ERASEBKGND:
				//    IntPtr hdc = m.WParam;
				//    base.WndProc(ref m);
				//    if (hdc != IntPtr.Zero)
				//    {
				//        using (Graphics g = Graphics.FromHdc(hdc))
				//        {
				//            DrawSearchMarks(g, clipRect);
				//        }
				//    }
				//    break;

				case WM_PAINT:
					Rectangle clipRect = User32.GetUpdateRect(Handle, false).ToRectangle();
					base.WndProc(ref m);
					Paint(clipRect);
					break;

				default:
					base.WndProc(ref m);
					break;
			}
		}

		private void HandleUserInput()
		{
            string text = _textView.View.GetSelectedText();
			
			if (text != _previousSelectedText)
			{
				_previousSelectedText = text;
				SelectionChanged(text);
			}
		}

		private void Paint(Rectangle clipRect)
		{
			User32.HideCaret(Handle);

			using (Graphics g = Graphics.FromHwnd(Handle))
			{
				g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
				g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

				DrawSearchMarks(g, clipRect);
			}

			//IntPtr hdc = User32.GetDC(Handle);
			//if (hdc != IntPtr.Zero)
			//{
			//    using (Graphics g = Graphics.FromHdc(hdc))
			//    {
			//        DrawSearchMarks(g, clipRect);
			//    }

			//    User32.ReleaseDC(Handle, hdc);
			//}

			User32.ShowCaret(Handle);

			_markUpdateRect.Validate();
		}

		private void DrawSearchMarks(Graphics g, Rectangle clipRect)
		{
			//Fix for clip bounds: take into account left margin pane during horizontal scrolling.
            Point leftTop = _textView.GetPointOfLineColumn(_viewRange.iStartLine, leftTextColumnInView);
			if (!leftTop.IsEmpty)
			{
				_leftMarginWidth = leftTop.X;
			}

			if (clipRect == Rectangle.Empty)
			{
				clipRect = Rectangle.Truncate(g.VisibleClipBounds);
			}

			clipRect.X = Math.Max(clipRect.X, _leftMarginWidth);

			g.SetClip(clipRect);
			
            DrawRectangles(_freezeMarks1, AddinSettings.Instance.FreezeMark1BorderColor, g);
            DrawRectangles(_freezeMarks2, AddinSettings.Instance.FreezeMark2BorderColor, g);
            DrawRectangles(_freezeMarks3, AddinSettings.Instance.FreezeMark3BorderColor, g);
			DrawRectangles(_searchMarks, AddinSettings.Instance.SearchMarkBorderColor, g);
		}

		private void DrawRectangles(MarkCollection marks, Color penColor, Graphics g)
		{
            Rectangle[] rectangles = marks.GetRectanglesForVisibleMarks(_visibleTextStart, _visibleTextEnd, _textView);

			if (rectangles != null && rectangles.Length > 0)
			{
				using (var pen = new Pen(penColor))
				{
					g.DrawRectangles(pen, rectangles);
				}
			}
		}

		private void MarkModifiedHandler(object sender, MarkEventArgs e)
		{
			InvalidateMark(e.Mark);
		}

		private void MarkDeletedHandler(object sender, MarkEventArgs e)
		{
			InvalidateMark(e.Mark);
		}

		private void InvalidateMark(TextMark mark)
		{
			if (mark.IsVisible(_visibleTextStart, _visibleTextEnd))
			{
                Rectangle rect = mark.GetRectangle(_textView);
				_markUpdateRect.IncludeRectangle(rect);
			}
		}

		private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
		{
            _textView.ResetPointCache();

			if (e.ScrollInfo.IsHorizontal)
			{
				leftTextColumnInView = e.ScrollInfo.firstVisibleUnit;
			}

			if (e.ScrollInfo.IsVertical)
			{
				int topTextLineInView = 0;
				int bottomTextLineInView = 0;

                IVsLayeredTextView viewLayer = _textView.View as IVsLayeredTextView;
				IVsTextLayer topLayer = null;
                IVsTextLayer bufferLayer = _textView.Buffer as IVsTextLayer;

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
                    TextSpan entireSpan = _textView.Buffer.CreateSpanForAllLines();
					topTextLineInView = entireSpan.iStartLine;
					bottomTextLineInView = entireSpan.iEndLine;
				}

                TextSpan viewRange = _textView.Buffer.CreateSpanForAllLines();
				viewRange.iStartLine = topTextLineInView;
				if (bottomTextLineInView < viewRange.iEndLine)
				{
					viewRange.iEndLine = bottomTextLineInView;
					viewRange.iEndIndex = 0;
				}

				_viewRange = viewRange;

                _visibleTextStart = _textView.Buffer.GetPositionOfLineIndex(_viewRange.iStartLine, _viewRange.iStartIndex);
                _visibleTextEnd = _textView.Buffer.GetPositionOfLineIndex(_viewRange.iEndLine, _viewRange.iEndIndex);
			}
		}

		private void StreamTextChangedHandler(object sender, StreamTextChangedEventArgs e)
		{
			SearchInChangedText(_freezeSearch1, _freezeMarks1, e, _freezeText1);
			SearchInChangedText(_freezeSearch2, _freezeMarks2, e, _freezeText2);
			SearchInChangedText(_freezeSearch3, _freezeMarks3, e, _freezeText3);

			_markUpdateRect.Invalidate();
		}

		private void SearchInChangedText(TextSearch searcher, MarkCollection marks, StreamTextChangedEventArgs e, string searchText)
		{
			if (!string.IsNullOrEmpty(searchText))
			{
				int searchStart = e.Position - searchText.Length;
				int searchEnd = e.Position + e.NewLength + searchText.Length;

				var newMarks = searcher.SearchOccurrences(searchText, searchStart, searchEnd);

				int replacementStart = e.Position;
				int replacementEnd = e.Position + e.OldLength;

				marks.ReplaceMarks(newMarks, replacementStart, replacementEnd, e.NewLength - e.OldLength);
			}
		}

		private void RefreshFreezeGroups()
		{
			_freezeMarks1.ReplaceMarks(_freezeSearch1.SearchOccurrences(_freezeText1, _visibleTextStart, _visibleTextEnd));
			_freezeMarks2.ReplaceMarks(_freezeSearch2.SearchOccurrences(_freezeText2, _visibleTextStart, _visibleTextEnd));
			_freezeMarks3.ReplaceMarks(_freezeSearch3.SearchOccurrences(_freezeText3, _visibleTextStart, _visibleTextEnd));

			_freezeSearch1.SearchOccurrencesDelayed(_freezeText1, 0, int.MaxValue);
			_freezeSearch2.SearchOccurrencesDelayed(_freezeText2, 0, int.MaxValue);
			_freezeSearch3.SearchOccurrencesDelayed(_freezeText3, 0, int.MaxValue);

			_markUpdateRect.Invalidate();
		}

		private void SelectionChanged(string text)
		{
			_selectedText = text;

			_searchMarks.Clear();

			if (!string.IsNullOrEmpty(_selectedText))
			{
				var marks = _search.SearchOccurrences(_selectedText, _visibleTextStart, _visibleTextEnd);
				_searchMarks.ReplaceMarks(marks);
				_search.SearchOccurrencesDelayed(_selectedText, 0, int.MaxValue);
			}

			_markUpdateRect.Invalidate();
		}

		private void searcher_SearchCompleted(object sender, SearchCompletedEventArgs e)
		{
			if (e.Text == _selectedText)
			{
				_searchMarks.ReplaceMarks(e.Marks);
				_markUpdateRect.Invalidate();
			}
		}

		private void FreezeSearchCompleted1(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks1.ReplaceMarks(e.Marks);
			_markUpdateRect.Invalidate();
		}

		private void FreezeSearchCompleted2(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks2.ReplaceMarks(e.Marks);
			_markUpdateRect.Invalidate();
		}

		private void FreezeSearchCompleted3(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks3.ReplaceMarks(e.Marks);
			_markUpdateRect.Invalidate();
		}

		private void GotFocusHandler(object sender, ViewFocusEventArgs e)
		{
			EventHandler evt = GotFocus;
			if (evt != null) evt(this, EventArgs.Empty);
		}

		private void LostFocusHandler(object sender, ViewFocusEventArgs e)
		{
			EventHandler evt = LostFocus;
			if (evt != null) evt(this, EventArgs.Empty);
		}

		public void FreezeSearch(int group)
		{
			switch (group)
			{
				case 1:
					_freezeText1 = _selectedText;
					break;
				case 2:
					_freezeText2 = _selectedText;
					break;
				case 3:
					_freezeText3 = _selectedText;
					break;
			}

			RefreshFreezeGroups();
		}
	}
}
