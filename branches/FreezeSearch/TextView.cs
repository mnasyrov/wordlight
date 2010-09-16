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
	public class TextView : IDisposable
	{
		private class TextPoint
		{
			public int Line;
			public int Column;

			public override int GetHashCode()
			{
				return Line.GetHashCode() ^ Column.GetHashCode();
			}
		}

		private IVsTextView _view;
		private IVsTextLines _buffer;
		private TextViewEventAdapter _viewEvents;

		private int _lineHeight;

		private Dictionary<TextPoint, Point> _pointCache = new Dictionary<TextPoint, Point>();
		private object _pointCacheSync = new object();

		private Dictionary<TextSpan, Rectangle> _rectangleCache = new Dictionary<TextSpan, Rectangle>();
		private object _rectSpanCacheSync = new object();

		private TextSpan _visibleSpan = new TextSpan();
		private int _visibleTextStart;
		private int _visibleTextEnd;
		private int _visibleLeftTextColumn = 0;

		#region Properties

		public IVsTextView View
		{
			get { return _view; }
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
		}

		public void Dispose()
		{
			_viewEvents.ScrollChanged -= ScrollChangedHandler;
			_viewEvents.Dispose();
		}

		public Point GetScreenPoint(int line, int column)
		{
			var textPos = new TextPoint { Line = line, Column = column };
			var screenPoint = Point.Empty;

			lock (_pointCacheSync)
			{
				if (_pointCache.ContainsKey(textPos))
				{
					screenPoint = _pointCache[textPos];
				}
				else
				{
					var p = new Microsoft.VisualStudio.OLE.Interop.POINT[1];
					_view.GetPointOfLineColumn(line, column, p);

					screenPoint.X = p[0].x;
					screenPoint.Y = p[0].y;

					_pointCache.Add(textPos, screenPoint);
				}
			}

			return screenPoint;
		}

		public Rectangle GetRectangle(TextMark mark)
		{
			TextSpan span = new TextSpan();
			_buffer.GetLineIndexOfPosition(mark.Start, out span.iStartLine, out span.iStartIndex);
			_buffer.GetLineIndexOfPosition(mark.End, out span.iEndLine, out span.iEndIndex);

			return GetRectangle(span);
		}

		public Rectangle GetRectangle(TextSpan span)
		{
			var rect = Rectangle.Empty;

			lock (_rectSpanCacheSync)
			{
				if (_rectangleCache.ContainsKey(span))
				{
					rect = _rectangleCache[span];
				}
				else
				{
					rect = GetRectangleForSpanInternal(span);
					_rectangleCache.Add(span, rect);
				}
			}

			return rect;
		}

		private Rectangle GetRectangleForSpanInternal(TextSpan span)
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

		public bool IsVisible(TextMark mark)
		{
			return VisibleTextStart <= mark.End && mark.Start <= VisibleTextEnd;
		}

		public void ResetCaches()
		{
			lock (_pointCacheSync)
			{
				lock (_rectSpanCacheSync)
				{
					_pointCache.Clear();
					_rectangleCache.Clear();
				}
			}
		}

		private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
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

				_visibleSpan = viewRange;

				_visibleTextStart = _buffer.GetPositionOfLineIndex(_visibleSpan.iStartLine, _visibleSpan.iStartIndex);
				_visibleTextEnd = _buffer.GetPositionOfLineIndex(_visibleSpan.iEndLine, _visibleSpan.iEndIndex);
			}

			if (e.ScrollInfo.IsHorizontal || e.ScrollInfo.IsVertical)
			{
				ResetCaches();
			}
		}
	}
}
