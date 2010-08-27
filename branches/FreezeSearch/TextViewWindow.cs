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
        
        private IVsTextView _view;
        private IVsTextLines _buffer;
        private IVsHiddenTextManager _hiddenTextManager;
        private string _previousSelectedText;

		private List<TextSpan> _searchMarks = new List<TextSpan>();
        private object _searchMarksSyncLock = new object();

		private List<TextSpan> _freezeMarks1 = new List<TextSpan>();
		private object _freezeMarks1SyncLock = new object();

		private List<TextSpan> _freezeMarks2 = new List<TextSpan>();
		private object _freezeMarks2SyncLock = new object();

		private List<TextSpan> _freezeMarks3 = new List<TextSpan>();
		private object _freezeMarks3SyncLock = new object(); 

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
            using (Graphics g = Graphics.FromHwnd(this.Handle))
            {
                DrawSearchMarks(g);
            }
        }

        private void RepaintWindow()
        {
            InvalidateRect(Handle, IntPtr.Zero, true);
            UpdateWindow(Handle);
        }

        private void UpdateVisibleMarks()
        {
            RectangleF maxClipBounds = new RectangleF(0, 0, float.MaxValue, float.MaxValue);
            List<Rectangle> rectList;
            lock (_searchMarksSyncLock)
            {
                rectList = GetRectanglesForVisibleMarks(_searchMarks, maxClipBounds);
            }
            if (rectList.Count > 0)
            {
                foreach (var rect in rectList)
                {
                    IntPtr pRect = Marshal.AllocHGlobal(Marshal.SizeOf(rect));
                    try
                    {
                        Marshal.StructureToPtr(rect, pRect, false);
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
                _leftMarginWidth = leftTop.X;
            RectangleF visibleClipBounds = new RectangleF(
                _leftMarginWidth, g.VisibleClipBounds.Y, g.VisibleClipBounds.Width, g.VisibleClipBounds.Height);            

            List<Rectangle> rectList;
            lock (_searchMarksSyncLock)
            {
                rectList = GetRectanglesForVisibleMarks(_searchMarks, visibleClipBounds);
            }

			List<Rectangle> freezed1;
			lock (_freezeMarks1SyncLock)
			{
				freezed1 = GetRectanglesForVisibleMarks(_freezeMarks1, visibleClipBounds);
			}

			List<Rectangle> freezed2;
			lock (_freezeMarks2SyncLock)
			{
				freezed2 = GetRectanglesForVisibleMarks(_freezeMarks2, visibleClipBounds);
			}

			List<Rectangle> freezed3;
			lock (_freezeMarks3SyncLock)
			{
				freezed3 = GetRectanglesForVisibleMarks(_freezeMarks3, visibleClipBounds);
			}

            if (rectList.Count > 0)
            {
                Pen pen = new Pen(AddinSettings.Instance.SearchMarkBorderColor);
                g.DrawRectangles(pen, rectList.ToArray());
            }

			if (freezed1.Count > 0)
			{
				Pen pen = new Pen(Color.Blue);
				g.DrawRectangles(pen, freezed1.ToArray());
			}
			if (freezed2.Count > 0)
			{
				Pen pen = new Pen(Color.Green);
				g.DrawRectangles(pen, freezed2.ToArray());
			}
			if (freezed3.Count > 0)
			{
				Pen pen = new Pen(Color.Red);
				g.DrawRectangles(pen, freezed3.ToArray());
			}
        }

        private List<Rectangle> GetRectanglesForVisibleMarks(IList<TextSpan> marks, RectangleF visibleClipBounds)
        {
            List<Rectangle> rectList = new List<Rectangle>(marks.Count);

                foreach (TextSpan mark in marks)
                {
                    if (_viewRange.iStartLine <= mark.iEndLine && mark.iStartLine <= _viewRange.iEndLine)
                    {
                        Rectangle rect = GetVisibleRectangle(mark, _view, visibleClipBounds, _lineHeight);
                        if (rect != Rectangle.Empty)
                            rectList.Add((Rectangle)rect);
                    }
                }

            return rectList;
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

			_freezeMarks1.Clear();
			_freezeMarks1.AddRange(_freezeSearch1.SearchOccurrences(_freezeText1, _viewRange));
			_freezeSearch1.SearchOccurrencesDelayed(_freezeText1, document);

			_freezeMarks2.Clear();
			_freezeMarks2.AddRange(_freezeSearch2.SearchOccurrences(_freezeText2, _viewRange));
			_freezeSearch2.SearchOccurrencesDelayed(_freezeText2, document);

			_freezeMarks3.Clear();
			_freezeMarks3.AddRange(_freezeSearch3.SearchOccurrences(_freezeText3, _viewRange));
			_freezeSearch3.SearchOccurrencesDelayed(_freezeText3, document);
		}

        private void SelectionChanged(string text)
        {
			_selectedText = text;

            lock (_searchMarksSyncLock)
            {
                _searchMarks.Clear();
            }

			if (!string.IsNullOrEmpty(_selectedText))
            {
				var marks = _search.SearchOccurrences(_selectedText, _viewRange);
				lock (_searchMarksSyncLock)
				{
					_searchMarks.AddRange(marks);
				}
				_search.SearchOccurrencesDelayed(_selectedText, _buffer.CreateSpanForAllLines());
            }
            RepaintWindow();
        }

        private void searcher_SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            if (e.Text == _selectedText)
            {
                lock (_searchMarksSyncLock)
                {
                    _searchMarks.Clear();
                    _searchMarks.AddRange(e.Marks);
                }
                UpdateVisibleMarks();
            }
        }

		private void FreezeSearchCompleted1(object sender, SearchCompletedEventArgs e)
		{
			lock (_freezeMarks1SyncLock)
			{
				_freezeMarks1.Clear();
				_freezeMarks1.AddRange(e.Marks);
			}
			RepaintWindow();
		}

		private void FreezeSearchCompleted2(object sender, SearchCompletedEventArgs e)
		{
			lock (_freezeMarks2SyncLock)
			{
				_freezeMarks2.Clear();
				_freezeMarks2.AddRange(e.Marks);
			}
			RepaintWindow();
		}

		private void FreezeSearchCompleted3(object sender, SearchCompletedEventArgs e)
		{
			lock (_freezeMarks3SyncLock)
			{
				_freezeMarks3.Clear();
				_freezeMarks3.AddRange(e.Marks);
			}
			RepaintWindow();
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
                int x = Math.Max(startPoint.X, (int)visibleClipBounds.Left);
                int y = startPoint.Y;

                int height = endPoint.Y - y + lineHeight;
                int width = endPoint.X - x;                

                rect = new Rectangle(x, y, width, height);
            }

            return rect;
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
		}
    }
}