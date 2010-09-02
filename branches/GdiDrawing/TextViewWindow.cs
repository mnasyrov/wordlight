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
        
        private IVsTextView _view;
        private IVsTextLines _buffer;
        private IVsHiddenTextManager _hiddenTextManager;
        private string _previousSelectedText;

        private MarkCollection _searchMarks = new MarkCollection();
        private MarkCollection _freezeMarks1 = new MarkCollection();
        private MarkCollection _freezeMarks2 = new MarkCollection();
        private MarkCollection _freezeMarks3 = new MarkCollection();

        private int _lineHeight;

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

        public TextViewWindow(IVsTextView view, IVsHiddenTextManager hiddenTextManager)
        {
            if (view == null) throw new ArgumentNullException("view");
            if (hiddenTextManager == null) throw new ArgumentNullException("hiddenTextManager");
            
            _view = view;
            _hiddenTextManager = hiddenTextManager;

            _lineHeight = _view.GetLineHeight();
            _buffer = view.GetBuffer();
			_textStreamEvents = new TextStreamEventAdapter(_buffer);
			_textStreamEvents.StreamTextChanged += new EventHandler(StreamTextChangedHandler);

            _search = new TextSearch(_buffer);
            _search.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(searcher_SearchCompleted);

			_freezeSearch1 = new TextSearch(_buffer);
			_freezeSearch1.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted1);

			_freezeSearch2 = new TextSearch(_buffer);
			_freezeSearch2.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted2);

			_freezeSearch3 = new TextSearch(_buffer);
			_freezeSearch3.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted3);

            _viewEvents = new TextViewEventAdapter(_view);
            _viewEvents.ScrollChanged += new EventHandler<ViewScrollChangedEventArgs>(ScrollChangedHandler);
			_viewEvents.GotFocus += new EventHandler<ViewFocusEventArgs>(GotFocusHandler);
			_viewEvents.LostFocus += new EventHandler<ViewFocusEventArgs>(LostFocusHandler);

            AssignHandle(view.GetWindowHandle());
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

                case WM_ERASEBKGND:
					base.WndProc(ref m);
                    break;

                case WM_PAINT:
                    base.WndProc(ref m);                    
                    Paint();
                    break;

                default:
                    base.WndProc(ref m);
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

		//private void Paint()
		//{
		//    var ps = new User32.PAINTSTRUCT();
		//    IntPtr hdc = User32.BeginPaint(Handle, ref ps);

		//    if (hdc != IntPtr.Zero)
		//    {
		//        //using (Graphics g = Graphics.FromHdc(hdc))
		//        using (Graphics g = Graphics.FromHwnd(Handle))
		//        {
		//            DrawSearchMarks(g);
		//        }
		//    }

		//    User32.EndPaint(Handle, ref ps);

		//    //IntPtr pRect = Marshal.AllocHGlobal(Marshal.SizeOf(ps.rcPaint));
		//    //try
		//    //{
		//    //    Marshal.StructureToPtr(ps.rcPaint, pRect, false);
		//    //    InvalidateRect(Handle, pRect, false);
		//    //}
		//    //finally
		//    //{
		//    //    Marshal.FreeHGlobal(pRect);
		//    //}
		//}

		private void Paint()
		{
			IntPtr hdc = User32.GetDC(Handle);

			if (hdc != IntPtr.Zero)
			{
				var clientRect = User32.GetClientRect(Handle);

				int width = clientRect.Right - clientRect.Left;
				int height = clientRect.Bottom - clientRect.Top;

				IntPtr memDC = Gdi32.CreateCompatibleDC(hdc);

				IntPtr bmp = Gdi32.CreateCompatibleBitmap(hdc, width, height);
				Gdi32.SelectObject(memDC, bmp);

				//Gdi32.BitBlt(memDC, 0, 0, width, height, hdc, 0, 0, Gdi32.TernaryRasterOperations.SRCCOPY);

				DrawSearchMarks(memDC);

				Gdi32.BitBlt(hdc, 0, 0, width, height, memDC, 0, 0, Gdi32.TernaryRasterOperations.SRCCOPY);

				Gdi32.DeleteObject(bmp);
				Gdi32.DeleteDC(memDC);

				User32.ReleaseDC(Handle, hdc);
			}
		}

		//private void Paint()
		//{
		//    IntPtr hdc = User32.GetDC(Handle);

		//    if (hdc != IntPtr.Zero)
		//    {
		//        DrawSearchMarks(hdc);
		//        User32.ReleaseDC(Handle, hdc);
		//    }
		//}

		private void DrawSearchMarks(IntPtr hdc)
		{
			//Fix for clip bounds: take into account left margin pane during horizontal scrolling.
			Point leftTop = _view.GetPointOfLineColumn(_viewRange.iStartLine, leftTextColumnInView);
			if (!leftTop.IsEmpty)
			{
				_leftMarginWidth = leftTop.X;
			}

			//Rectangle visibleClipBounds =
			//    new Rectangle(_leftMarginWidth, (int)g.VisibleClipBounds.Y, (int)g.VisibleClipBounds.Width, (int)g.VisibleClipBounds.Height);
			var visibleClipBounds = new Rectangle(_leftMarginWidth, 0, int.MaxValue / 2, int.MaxValue / 2);

			Rectangle[] searchMarks = _searchMarks.GetRectanglesForVisibleMarks(_viewRange, visibleClipBounds, _view, _lineHeight);
			Rectangle[] freezed1 = _freezeMarks1.GetRectanglesForVisibleMarks(_viewRange, visibleClipBounds, _view, _lineHeight);
			Rectangle[] freezed2 = _freezeMarks2.GetRectanglesForVisibleMarks(_viewRange, visibleClipBounds, _view, _lineHeight);
			Rectangle[] freezed3 = _freezeMarks3.GetRectanglesForVisibleMarks(_viewRange, visibleClipBounds, _view, _lineHeight);

			DrawRectangles(hdc, searchMarks, AddinSettings.Instance.SearchMarkBorderColor);
			DrawRectangles(hdc, freezed1, Color.Aqua);
			DrawRectangles(hdc, freezed2, Color.Lime);
			DrawRectangles(hdc, freezed3, Color.Orange);
		}

		private void DrawRectangles(IntPtr hdc, Rectangle[] rectangles, Color penColor)
		{
			if (rectangles != null && rectangles.Length > 0)
			{
				IntPtr originalObject = Gdi32.SelectObject(hdc, Gdi32.GetStockObject(Gdi32.StockObjects.DC_PEN));

				Gdi32.SelectObject(hdc, Gdi32.GetStockObject(Gdi32.StockObjects.NULL_BRUSH));
				Gdi32.SelectObject(hdc, Gdi32.GetStockObject(Gdi32.StockObjects.DC_PEN));

				int originalPenColor = Gdi32.SetDCPenColor(hdc, ColorTranslator.ToWin32(penColor));

				for (int i = 0; i < rectangles.Length; i++)
				{
					var rect = rectangles[i];
					Gdi32.Rectangle(hdc, rect.Left, rect.Top, rect.Right, rect.Bottom);
				}

				Gdi32.SetDCPenColor(hdc, originalPenColor);
				Gdi32.SelectObject(hdc, originalObject);
			}
		}

        private void RepaintWindow()
        {
			User32.InvalidateRect(Handle, IntPtr.Zero, true);
			User32.UpdateWindow(Handle);
        }

        private void InvalidateVisibleMarks(MarkCollection marks)
        {
            Rectangle maxClipBounds = new Rectangle(0, 0, int.MaxValue, int.MaxValue);

            Rectangle[] rectList = marks.GetRectanglesForVisibleMarks(_viewRange, maxClipBounds, _view, _lineHeight);

            if (rectList != null)
            {
				var paintRect = new RECT();

                for(int i = 0; i < rectList.Length; i++)
                {
                    var rect = rectList[i];

                    paintRect.Bottom = rect.Bottom;
                    paintRect.Left = rect.Left;
                    paintRect.Right = rect.Right;
                    paintRect.Top = rect.Top;

                    IntPtr pRect = Marshal.AllocHGlobal(Marshal.SizeOf(paintRect));
                    try
                    {
                        Marshal.StructureToPtr(paintRect, pRect, false);
						User32.InvalidateRect(Handle, pRect, false);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pRect);
                    }
                }
            }
        }
        
        private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
        {
            if (e.ScrollInfo.IsHorizontal)
            {
                leftTextColumnInView = e.ScrollInfo.firstVisibleUnit;
            }
            if (e.ScrollInfo.IsVertical)
            {
				int topTextLineInView = 0;
				int bottomTextLineInView = 0;

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

				TextSpan viewRange = _buffer.CreateSpanForAllLines();
				viewRange.iStartLine = topTextLineInView;
				if (bottomTextLineInView < viewRange.iEndLine)
				{
					viewRange.iEndLine = bottomTextLineInView;
					viewRange.iEndIndex = 0;
				}

				_viewRange = viewRange;
            }
        }

		private void StreamTextChangedHandler(object sender, EventArgs e)
		{
			RefreshFreezeGroups();

			RepaintWindow();
		}

		private void RefreshFreezeGroups()
		{
			TextSpan document = _buffer.CreateSpanForAllLines();

			_freezeMarks1.ReplaceMarks(_freezeSearch1.SearchOccurrences(_freezeText1, _viewRange));
            _freezeSearch1.SearchOccurrencesDelayed(_freezeText1, document);

            _freezeMarks2.ReplaceMarks(_freezeSearch2.SearchOccurrences(_freezeText2, _viewRange));
            _freezeSearch2.SearchOccurrencesDelayed(_freezeText2, document);

            _freezeMarks3.ReplaceMarks(_freezeSearch3.SearchOccurrences(_freezeText3, _viewRange));
			_freezeSearch3.SearchOccurrencesDelayed(_freezeText3, document);
		}

        private void SelectionChanged(string text)
        {
			_selectedText = text;
            
            _searchMarks.Clear();

			if (!string.IsNullOrEmpty(_selectedText))
            {
				var marks = _search.SearchOccurrences(_selectedText, _viewRange);
                _searchMarks.ReplaceMarks(marks);
				_search.SearchOccurrencesDelayed(_selectedText, _buffer.CreateSpanForAllLines());
            }
            RepaintWindow();
        }

        private void searcher_SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            if (e.Text == _selectedText)
            {
                _searchMarks.ReplaceMarks(e.Marks);
				//InvalidateVisibleMarks(_searchMarks);
				//User32.UpdateWindow(Handle);

				//if (_viewRange.iStartLine != e.Range.iStartLine || _viewRange.iEndLine != e.Range.iEndLine)
				//    RepaintWindow();
            }
        }

		private void FreezeSearchCompleted1(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks1.ReplaceMarks(e.Marks);
            //InvalidateVisibleMarks(_freezeMarks1);
			
			//if (_viewRange.iStartLine != e.Range.iStartLine || _viewRange.iEndLine != e.Range.iEndLine)
			//    RepaintWindow();
		}

		private void FreezeSearchCompleted2(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks2.ReplaceMarks(e.Marks);
            //InvalidateVisibleMarks(_freezeMarks2);

			//if (_viewRange.iStartLine != e.Range.iStartLine || _viewRange.iEndLine != e.Range.iEndLine)
			//    RepaintWindow();
		}

		private void FreezeSearchCompleted3(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks3.ReplaceMarks(e.Marks);
            //InvalidateVisibleMarks(_freezeMarks3);

			//if (_viewRange.iStartLine != e.Range.iStartLine || _viewRange.iEndLine != e.Range.iEndLine)
			//    RepaintWindow();
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
				case 1 :
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
            RepaintWindow();
		}
    }
}
