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

        private int _lineHeight;

        private string _selectedText;
        private TextViewEventAdapter _viewEvents;

        private int topTextLineInView = 0;
        private int bottomTextLineInView = 0;

        private TextSearch _search;

        private IVsTextManager _textManager;
        private int _searchMarkerTypeId;

        private List<IVsTextLineMarker> searchMarkers = new List<IVsTextLineMarker>();
        private object searchMarkersSyncLock = new object();

        public TextViewWindow(IVsTextView view, IVsHiddenTextManager hiddenTextManager, IVsTextManager textManager)
        {
            if (view == null) throw new ArgumentNullException("view");
            if (hiddenTextManager == null) throw new ArgumentNullException("hiddenTextManager");

            _textManager = textManager;
            Guid searchMarkerType = GuidConstants.SearchMarkerType;
            _textManager.GetRegisteredMarkerTypeID(ref searchMarkerType, out _searchMarkerTypeId);
            
            _view = view;
            _hiddenTextManager = hiddenTextManager;

            _lineHeight = _view.GetLineHeight();
            _buffer = view.GetBuffer();

            _search = new TextSearch(_buffer);
            _search.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(searcher_SearchCompleted);            

            _viewEvents = new TextViewEventAdapter(_view);
            _viewEvents.ScrollChanged += new EventHandler<ViewScrollChangedEventArgs>(ScrollChangedHandler);

            AssignHandle(view.GetWindowHandle());
        }

        public void Dispose()
        {
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

        private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
        {
            if (e.ScrollInfo.IsVertical)
            {
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
            }
        }

        private void SelectionChanged(string text)
        {
            _selectedText = text;

            lock (searchMarkersSyncLock)
            {
                foreach (var marker in searchMarkers)
                {
                    marker.UnadviseClient();
                    marker.Invalidate();
                }
                searchMarkers.Clear();
            }

            if (!string.IsNullOrEmpty(text))
            {
                SearchWords();
            }
        }

        private void SearchWords()
        {
            TextSpan viewRange = _buffer.CreateSpanForAllLines();
            viewRange.iStartLine = topTextLineInView;
			if (bottomTextLineInView < viewRange.iEndLine)
            {
                viewRange.iEndLine = bottomTextLineInView;
                viewRange.iEndIndex = 0;
            }

            IList<TextSpan> marks = _search.SearchOccurrences(_selectedText, viewRange);
            _search.SearchOccurrencesDelayed(_selectedText, _buffer.CreateSpanForAllLines());

            lock (searchMarkersSyncLock)
            {
                foreach (TextSpan mark in marks)
                {
                    var marker = new IVsTextLineMarker[1];
                    _buffer.CreateLineMarker(_searchMarkerTypeId, mark.iStartLine, mark.iStartIndex, mark.iEndLine, mark.iEndIndex, null, marker);
                    searchMarkers.Add(marker[0]);
                }
            }
        }

        private void searcher_SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            if (e.Text == _selectedText)
            {
                lock (searchMarkersSyncLock)
                {
                    foreach (TextSpan mark in e.Marks)
                    {
                        var marker = new IVsTextLineMarker[1];
                        _buffer.CreateLineMarker(_searchMarkerTypeId, mark.iStartLine, mark.iStartIndex, mark.iEndLine, mark.iEndIndex, null, marker);
                        searchMarkers.Add(marker[0]);
                    }
                }
            }
        }
    }
}
