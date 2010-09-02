using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
    public class MarkCollection
    {
        private TextSpan[] _marks = null;
        private object _marksSyncRoot = new object();

        public void Clear()
        {
            lock (_marksSyncRoot)
            {
                _marks = null;
            }
        }

        public void ReplaceMarks(TextSpan[] newMarks)
        {
            lock (_marksSyncRoot)
            {
                _marks = newMarks;
            }
        }

        public Rectangle[] GetRectanglesForVisibleMarks(TextSpan viewRange, Rectangle visibleClipBounds, IVsTextView view, int lineHeight)
        {
            List<Rectangle> rectList = null;

            lock (_marksSyncRoot)
            {
                if (_marks != null)
                {
                    for (int i = 0; i < _marks.Length && _marks[i].iStartLine <= viewRange.iEndLine; i++)
                    {
                        TextSpan mark = _marks[i];

                        if (mark.iEndLine < viewRange.iStartLine)
                            continue;

                        //GetVisibleRectangle
                        Point startPoint = view.GetPointOfLineColumn(mark.iStartLine, mark.iStartIndex);
                        if (startPoint == Point.Empty)
                            continue;

                        Point endPoint = view.GetPointOfLineColumn(mark.iEndLine, mark.iEndIndex);
                        if (endPoint == Point.Empty)
                            continue;

                        bool isVisible =
                            visibleClipBounds.Left <= endPoint.X && startPoint.X <= visibleClipBounds.Right
                            && visibleClipBounds.Top <= endPoint.Y && startPoint.Y <= visibleClipBounds.Bottom;

                        if (isVisible)
                        {
                            int x = Math.Max(startPoint.X, visibleClipBounds.Left);
                            int y = startPoint.Y;

                            int height = endPoint.Y - y + lineHeight;
                            int width = endPoint.X - x;

                            if (rectList == null)
                                rectList = new List<Rectangle>();

                            rectList.Add(new Rectangle(x, y, width, height));
                        }
                    }
                }
            }

            if (rectList != null)
                return rectList.ToArray();

            return null;
        }
    }
}
