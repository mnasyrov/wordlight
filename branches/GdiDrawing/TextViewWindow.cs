﻿using System;
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
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

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

        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool ValidateRect(IntPtr hWnd, IntPtr rect);

        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool GetUpdateRect(IntPtr hWnd, IntPtr rect, bool erase);

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern IntPtr EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

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
                    //m.Msg = 0;
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

        private void Paint()
        {
            PAINTSTRUCT ps = new PAINTSTRUCT();
            IntPtr hdc = BeginPaint(Handle, ref ps);

            if (hdc != IntPtr.Zero)
            {
                //using (Graphics g = Graphics.FromHdc(hdc))
                using (Graphics g = Graphics.FromHwnd(Handle))
                {
                    DrawSearchMarks(g);
                }
            }

            EndPaint(Handle, ref ps);

            //IntPtr pRect = Marshal.AllocHGlobal(Marshal.SizeOf(ps.rcPaint));
            //try
            //{
            //    Marshal.StructureToPtr(ps.rcPaint, pRect, false);
            //    InvalidateRect(Handle, pRect, false);
            //}
            //finally
            //{
            //    Marshal.FreeHGlobal(pRect);
            //}
        }

        private void RepaintWindow()
        {
            InvalidateRect(Handle, IntPtr.Zero, true);
            UpdateWindow(Handle);
        }

        private void UpdateVisibleMarks(MarkCollection marks)
        {
            Rectangle maxClipBounds = new Rectangle(0, 0, int.MaxValue, int.MaxValue);

            Rectangle[] rectList = marks.GetRectanglesForVisibleMarks(_viewRange, maxClipBounds, _view, _lineHeight);

            if (rectList != null)
            {
                RECT paintRect = new RECT();

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

        private int _leftMarginWidth = 0;

        private void DrawSearchMarks(Graphics g)
        {
            //Fix for clip bounds: take into account left margin pane during horizontal scrolling.
            Point leftTop = _view.GetPointOfLineColumn(_viewRange.iStartLine, leftTextColumnInView);
            if (!leftTop.IsEmpty)
            {
                _leftMarginWidth = leftTop.X;
            }

            Rectangle visibleClipBounds = 
                new Rectangle(_leftMarginWidth, (int)g.VisibleClipBounds.Y, (int)g.VisibleClipBounds.Width, (int)g.VisibleClipBounds.Height);

            Rectangle[] rectList = _searchMarks.GetRectanglesForVisibleMarks(_viewRange, visibleClipBounds, _view, _lineHeight);
            Rectangle[] freezed1 = _freezeMarks1.GetRectanglesForVisibleMarks(_viewRange, visibleClipBounds, _view, _lineHeight);
            Rectangle[] freezed2 = _freezeMarks2.GetRectanglesForVisibleMarks(_viewRange, visibleClipBounds, _view, _lineHeight);
            Rectangle[] freezed3 = _freezeMarks3.GetRectanglesForVisibleMarks(_viewRange, visibleClipBounds, _view, _lineHeight);

            if (rectList != null)
            {
                var pen = new Pen(AddinSettings.Instance.SearchMarkBorderColor);
                g.DrawRectangles(pen, rectList);
            }
			if (freezed1 != null)
			{
				var pen = new Pen(Color.Blue);
				g.DrawRectangles(pen, freezed1);
			}
			if (freezed2 != null)
			{
				var pen = new Pen(Color.Green);
				g.DrawRectangles(pen, freezed2);
			}
			if (freezed3 != null)
			{
				var pen = new Pen(Color.Red);
				g.DrawRectangles(pen, freezed3);
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
                UpdateVisibleMarks(_searchMarks);
            }
        }

		private void FreezeSearchCompleted1(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks1.ReplaceMarks(e.Marks);
            UpdateVisibleMarks(_freezeMarks1);
		}

		private void FreezeSearchCompleted2(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks2.ReplaceMarks(e.Marks);
            UpdateVisibleMarks(_freezeMarks2);
		}

		private void FreezeSearchCompleted3(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks3.ReplaceMarks(e.Marks);
            UpdateVisibleMarks(_freezeMarks3);
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