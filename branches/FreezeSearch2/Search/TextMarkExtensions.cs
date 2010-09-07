using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
    public static class TextMarkExtensions
    {
        public static Rectangle GetRectangle(this TextMark mark, IVsTextView view, int lineHeight, IVsTextBuffer buffer)
        {
            TextSpan span = new TextSpan();
            buffer.GetLineIndexOfPosition(mark.Start, out span.iStartLine, out span.iStartIndex);
            buffer.GetLineIndexOfPosition(mark.End, out span.iEndLine, out span.iEndIndex);

            Point startPoint = view.GetPointOfLineColumn(span.iStartLine, span.iStartIndex);
            if (startPoint == Point.Empty)
                return Rectangle.Empty;

            Point endPoint = view.GetPointOfLineColumn(span.iEndLine, span.iEndIndex);
            if (endPoint == Point.Empty)
                return Rectangle.Empty;

            int x = startPoint.X;
            int y = startPoint.Y;

            int height = endPoint.Y - y + lineHeight;
            int width = endPoint.X - x;

            return new Rectangle(x, y, width, height);
        }
    }
}
