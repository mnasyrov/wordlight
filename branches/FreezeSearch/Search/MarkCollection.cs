using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
    public class MarkCollection
    {
        private List<TextSpan> _marks = new List<TextSpan>();
        private object _marksSyncRoot = new object();

        public void Clear()
        {
            lock (_marksSyncRoot)
            {
                _marks.Clear();
            }
        }

        public void AddMarks(IEnumerable<TextSpan> newMarks)
        {
            lock (_marksSyncRoot)
            {
                _marks.AddRange(newMarks);
            }
        }

        public void ReplaceMarks(IEnumerable<TextSpan> newMarks)
        {
            lock (_marksSyncRoot)
            {
                _marks.Clear();
                _marks.AddRange(newMarks);
            }
        }

        public List<Rectangle> GetRectanglesForVisibleMarks(TextSpan viewRange, RectangleF visibleClipBounds, IVsTextView view, int lineHeight)
        {
            lock (_marksSyncRoot)
            {

                List<Rectangle> rectList = new List<Rectangle>(_marks.Count);

                foreach (TextSpan mark in _marks)
                {
                    if (viewRange.iStartLine <= mark.iEndLine && mark.iStartLine <= viewRange.iEndLine)
                    {
                        Rectangle rect = GetVisibleRectangle(mark, view, visibleClipBounds, lineHeight);
                        if (rect != Rectangle.Empty)
                            rectList.Add((Rectangle)rect);
                    }
                }

                return rectList;
            }
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
    }
}
