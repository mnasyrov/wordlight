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
	public class TextView : IDisposable
	{
		private IVsTextView _view;
		private IVsTextLines _buffer;
		private TextViewEventAdapter _viewEvents;

		TextViewWindow _window;

		private int _lineHeight;

		private Dictionary<long, Point> _pointCache = new Dictionary<long, Point>();
		private object _pointCacheSync = new object();

		private TextSpan _visibleSpan = new TextSpan();
		private int _visibleTextStart;
		private int _visibleTextEnd;
		private int _visibleLeftTextColumn = 0;

		#region Properties

		//public IVsTextView View
		//{
		//    get { return _view; }
		//}

		public TextViewWindow Window
		{
			get { return _window; }
		}

		public IntPtr WindowHandle
		{
			get { return _view.GetWindowHandle(); }
		}

		public IVsTextLines Buffer
		{
			get { return _buffer; }
		}

		public TextViewEventAdapter ViewEvents
		{
			get { return _viewEvents; }
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

		#endregion

		public TextView(IVsTextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

			_view = view;
			_buffer = view.GetBuffer();
			
			_lineHeight = _view.GetLineHeight();

			_viewEvents = new TextViewEventAdapter(view);
			_viewEvents.ScrollChanged += ScrollChangedHandler;

			_window = new TextViewWindow(this);
		}

		public void Dispose()
		{
			_viewEvents.ScrollChanged -= ScrollChangedHandler;
			_viewEvents.Dispose();

			_window.Dispose();
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
			_buffer.GetLineIndexOfPosition(position, out line, out column);
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
                    IVsTextLayer bufferLayer = _buffer as IVsTextLayer;

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

                    _visibleSpan = viewRange;

                    _visibleTextStart = _buffer.GetPositionOfLineIndex(_visibleSpan.iStartLine, _visibleSpan.iStartIndex);
                    _visibleTextEnd = _buffer.GetPositionOfLineIndex(_visibleSpan.iEndLine, _visibleSpan.iEndIndex);
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

		public string GetSelectedText()
		{
			return _view.GetSelectedText();
		}
	}
}
