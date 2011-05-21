using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.NativeMethods;
using WordLight.EventAdapters;
using WordLight.Extensions;
using WordLight.Search;

namespace WordLight
{
    public class TextView : IDisposable, ITextViewAdapter
    {
        private IVsTextView _view;
        private IVsTextLines _buffer;
        private TextViewEventAdapter _viewEvents;
        private TextStreamEventAdapter _textStreamEvents;

        private TextViewWindow _window;
		private object _windowLock = new object();

        private ScreenUpdateManager _screenUpdater;

        private int _lineHeight = 0;

        private Dictionary<long, Point> _pointCache = new Dictionary<long, Point>();
        private object _pointCacheSync = new object();

        private TextSpan _visibleSpan = new TextSpan();
        private int _visibleTextStart;
        private int _visibleTextEnd;
        private int _visibleLeftTextColumn = 0;

        private MarkSearcher selectionSearcher;
        private MarkSearcher freezer1;
        private MarkSearcher freezer2;
        private MarkSearcher freezer3;
        private List<MarkSearcher> freezers;

        #region Properties

        public IntPtr WindowHandle
        {
            get { return _view.GetWindowHandle(); }
        }

        public IVsTextLines Buffer
        {
            get { return _buffer; }
        }

        public int LineHeight
        {
            get { return _lineHeight; }
        }

        public TextSpan VisibleSpan
        {
            get { return _visibleSpan; }
        }

        public int VisibleTextStart
        {
            get { return _visibleTextStart; }
        }

        public int VisibleTextEnd
        {
            get { return _visibleTextEnd; }
        }

        public int VisibleLeftTextColumn
        {
            get { return _visibleLeftTextColumn; }
        }

        public IScreenUpdateManager ScreenUpdater
        {
            get { return _screenUpdater; }
        }

        #endregion

        public TextView(IVsTextView view)
        {
            if (view == null) throw new ArgumentNullException("view");

            try
            {
                _view = view;
                _buffer = view.GetBuffer();

                _viewEvents = new TextViewEventAdapter(view);
                _textStreamEvents = new TextStreamEventAdapter(Buffer);

                _viewEvents.ScrollChanged += ScrollChangedHandler;
                _viewEvents.GotFocus += new EventHandler<ViewFocusEventArgs>(GotFocusHandler);

                _screenUpdater = new ScreenUpdateManager(this);

				CreateWindow();

                selectionSearcher = new MarkSearcher(-1, this);
                freezer1 = new MarkSearcher(1, this);
                freezer2 = new MarkSearcher(2, this);
                freezer3 = new MarkSearcher(3, this);

                freezers = new List<MarkSearcher>();
                freezers.Add(freezer1);
                freezers.Add(freezer2);
                freezers.Add(freezer3);

				_textStreamEvents.StreamTextChanged += new EventHandler<StreamTextChangedEventArgs>(StreamTextChanged);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to create TextView", ex);
            }
        }

		private void StreamTextChanged(object sender, StreamTextChangedEventArgs e)
		{
			foreach (MarkSearcher searcher in freezers)
			{
				searcher.OnTextChanged(e.Position, e.NewLength, e.Position, e.OldLength);
			}
		}

        public void Dispose()
        {
            _viewEvents.ScrollChanged -= ScrollChangedHandler;
            _viewEvents.GotFocus -= GotFocusHandler;
            _viewEvents.Dispose();

            _textStreamEvents.Dispose();

            if (_window != null)
            {
                _window.Paint -= _window_Paint;
                _window.PaintEnd -= _window_PaintEnd;
                _window.Dispose();
            }
        }

		#region IBufferTextProvider

		public string GetBufferText()
		{
			string text = string.Empty;
			if (_buffer != null)
			{
				text = _buffer.GetText();
			}
			return text;
		}

		#endregion

		private void CreateWindow()
		{
			if (_window == null && WindowHandle != IntPtr.Zero)
			{
				lock (_windowLock)
				{
					if (_window == null && WindowHandle != IntPtr.Zero)
					{
                        _lineHeight = _view.GetLineHeight();

						_window = new TextViewWindow(this);
						_window.Paint += new PaintEventHandler(_window_Paint);
						_window.PaintEnd += new EventHandler(_window_PaintEnd);
					}
				}
			}
		}

        public void SearchText(string text)
        {
            selectionSearcher.Search(text);
            _screenUpdater.RequestUpdate();
        }

        public Point GetScreenPoint(int line, int column)
        {
            long pointKey = ((_visibleSpan.iStartLine & 0xFFFFL) << 32) | ((line & 0xFFFFL) << 16) | (column & 0xFFFFL);
            var screenPoint = Point.Empty;

            lock (_pointCacheSync)
            {
                if (_pointCache.ContainsKey(pointKey))
                {
                    screenPoint = _pointCache[pointKey];
                }
                else
                {
                    var p = new Microsoft.VisualStudio.OLE.Interop.POINT[1];
                    _view.GetPointOfLineColumn(line, column, p);

                    screenPoint.X = p[0].x;
                    screenPoint.Y = p[0].y;

                    _pointCache.Add(pointKey, screenPoint);
                }
            }

            return screenPoint;
        }

        public Point GetScreenPointForTextPosition(int position)
        {
            int line;
            int column;
            Buffer.GetLineIndexOfPosition(position, out line, out column);
            return GetScreenPoint(line, column);
        }

        public Rectangle GetRectangleForMark(int markStart, int markLength)
        {
            Point startPoint = GetScreenPointForTextPosition(markStart);
            if (startPoint != Point.Empty)
            {
                Point endPoint = GetScreenPointForTextPosition(markStart + markLength);
                if (endPoint != Point.Empty)
                {
                    int x = startPoint.X;
                    int y = startPoint.Y;
                    int height = endPoint.Y - y + LineHeight;
                    int width = endPoint.X - startPoint.X;

                    return new Rectangle(x, y, width, height);
                }
            }
            return Rectangle.Empty;
        }

        public Rectangle GetRectangle(TextSpan span)
        {
            Point startPoint = GetScreenPoint(span.iStartLine, span.iStartIndex);
            if (startPoint == Point.Empty)
                return Rectangle.Empty;

            Point endPoint = GetScreenPoint(span.iEndLine, span.iEndIndex);
            if (endPoint == Point.Empty)
                return Rectangle.Empty;

            int x = startPoint.X;
            int y = startPoint.Y;
            int height = endPoint.Y - y + LineHeight;
            int width = endPoint.X - x;

            return new Rectangle(x, y, width, height);
        }

        public bool IsVisibleText(int position, int length)
        {
            return VisibleTextStart <= (position + length) && position <= VisibleTextEnd;
        }
        
        public string GetSelectedText()
        {
            return _view.GetSelectedText();
        }

        public void FreezeSearch(int group)
        {
			foreach (var freezer in freezers)
			{
				if (freezer.Id == group && freezer.SearchText != selectionSearcher.SearchText)
				{
					freezer.FreezeText(selectionSearcher.SearchText);
				}
				else if (freezer.Id != group && freezer.SearchText == selectionSearcher.SearchText)
				{
					freezer.Clear();
				}
			}

			_screenUpdater.RequestUpdate();
        }

        private void ResetCaches()
        {
            lock (_pointCacheSync)
            {
                _pointCache.Clear();
            }
        }

        private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
        {
            try
            {
                if (e.ScrollInfo.IsHorizontal)
                {
                    _visibleLeftTextColumn = e.ScrollInfo.firstVisibleUnit;
                }

                if (e.ScrollInfo.IsVertical)
                {
                    int topTextLineInView = 0;
                    int bottomTextLineInView = 0;

                    IVsLayeredTextView viewLayer = _view as IVsLayeredTextView;
                    IVsTextLayer topLayer = null;
                    IVsTextLayer bufferLayer = Buffer as IVsTextLayer;

                    if (viewLayer != null)
                    {
                        viewLayer.GetTopmostLayer(out topLayer);
                    }

                    if (topLayer != null && bufferLayer != null)
                    {
                        int lastVisibleUnit = Math.Min(e.ScrollInfo.firstVisibleUnit + e.ScrollInfo.visibleUnits, e.ScrollInfo.maxUnit);
                        int temp;
                        topLayer.LocalLineIndexToDeeperLayer(bufferLayer, e.ScrollInfo.firstVisibleUnit, 0, out topTextLineInView, out temp);
                        topLayer.LocalLineIndexToDeeperLayer(bufferLayer, lastVisibleUnit, 0, out bottomTextLineInView, out temp);
                        bottomTextLineInView++;
                    }
                    else
                    {
                        TextSpan entireSpan = Buffer.CreateSpanForAllLines();
                        topTextLineInView = entireSpan.iStartLine;
                        bottomTextLineInView = entireSpan.iEndLine;
                    }

                    TextSpan viewRange = Buffer.CreateSpanForAllLines();
                    viewRange.iStartLine = topTextLineInView;
                    if (bottomTextLineInView < viewRange.iEndLine)
                    {
                        viewRange.iEndLine = bottomTextLineInView;
                        viewRange.iEndIndex = 0;
                    }

                    _visibleSpan = viewRange;

                    _visibleTextStart = Buffer.GetPositionOfLineIndex(_visibleSpan.iStartLine, _visibleSpan.iStartIndex);
                    _visibleTextEnd = Buffer.GetPositionOfLineIndex(_visibleSpan.iEndLine, _visibleSpan.iEndIndex);
                }

                if (e.ScrollInfo.IsHorizontal || e.ScrollInfo.IsVertical)
                {
                    ResetCaches();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in scrollbar handler", ex);
            }
        }       

        private void GotFocusHandler(object sender, ViewFocusEventArgs e)
        {
			CreateWindow();
        }

        private void _window_Paint(object sender, PaintEventArgs e)
        {
			DrawMarks(e.Graphics, selectionSearcher.Marks, AddinSettings.Instance.SearchMarkBorderColor);
			DrawMarks(e.Graphics, freezer1.Marks, AddinSettings.Instance.FreezeMark1BorderColor);
			DrawMarks(e.Graphics, freezer2.Marks, AddinSettings.Instance.FreezeMark2BorderColor);
			DrawMarks(e.Graphics, freezer3.Marks, AddinSettings.Instance.FreezeMark3BorderColor);
        }

        private void DrawMarks(Graphics g, MarkCollection marks, Color markColor)
		{
            Rectangle[] rectangles = marks.GetRectanglesForVisibleMarks(this);

			if (rectangles == null || rectangles.Length == 0)
				return;

			if (AddinSettings.Instance.FilledMarks)
            {
				List<Rectangle> rectsToFilling = new List<Rectangle>();

				uint nativeBorderColor = (uint)markColor.R | (uint)markColor.G << 8 | (uint)markColor.B << 16;

				IntPtr hdc = g.GetHdc();

				for (int i = 0; i < rectangles.Length; i++)
				{
					var rect = rectangles[i];

                    int score = 0;
                    if (Gdi32.GetPixel(hdc, rect.Left, rect.Top) == nativeBorderColor) score++;
                    if (Gdi32.GetPixel(hdc, rect.Left, rect.Bottom) == nativeBorderColor) score++;
                    if (score < 2 && Gdi32.GetPixel(hdc, rect.Right, rect.Bottom) == nativeBorderColor) score++;
                    if (score < 2 && Gdi32.GetPixel(hdc, rect.Right, rect.Top) == nativeBorderColor) score++;

                    bool isBorderDrawn = score >= 2;

                    if (!isBorderDrawn)
                        rectsToFilling.Add(rect);
				}

				g.ReleaseHdc();

				if (rectsToFilling.Count > 0)
				{
					using (var bodyBrush = new SolidBrush(Color.FromArgb(32, markColor)))
						g.FillRectangles(bodyBrush, rectsToFilling.ToArray());

                    //using (var borderPen = new Pen(markColor))
                    //    g.DrawRectangles(borderPen, rectsToFilling.ToArray());
				}
            }

			//Draw borders
            using (var borderPen = new Pen(markColor))
                g.DrawRectangles(borderPen, rectangles);
        }

        private void _window_PaintEnd(object sender, EventArgs e)
        {
            _screenUpdater.CompleteUpdate();
        }
    }
}
