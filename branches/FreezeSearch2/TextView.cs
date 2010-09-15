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
    public class TextView
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
        private int _lineHeight;

        private Dictionary<TextPoint, Point> _pointCache = new Dictionary<TextPoint, Point>();
        private object _pointCacheSync = new object();

        private Dictionary<TextSpan, Rectangle> _rectangleCache = new Dictionary<TextSpan, Rectangle>();
        private object _rectSpanCacheSync = new object();

        public IVsTextView View
        {
            get { return _view; }
        }

        public IVsTextLines Buffer
        {
            get { return _buffer; }
        }

        public int LineHeight
        {
            get { return _lineHeight; }
        }

        public int VisibleTextStart { get; set; }
        public int VisibleTextEnd { get; set; }

        public TextView(IVsTextView view)
        {
            if (view == null) throw new ArgumentNullException("view");

            _view = view;
            _buffer = view.GetBuffer();
            _lineHeight = _view.GetLineHeight();
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

            //Don't display multiline marks
            if (span.iStartLine != span.iEndLine)
                return Rectangle.Empty;

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
    }
}
