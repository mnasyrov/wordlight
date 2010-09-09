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
		public static bool IsVisible(this TextMark mark, int visibleTextStart, int visibleTextEnd)
        {
			return visibleTextStart <= mark.End && mark.Position <= visibleTextEnd;
		}

        public static Rectangle GetRectangle(this TextMark mark, TextView view)
        {
            TextSpan span = new TextSpan();
            view.Buffer.GetLineIndexOfPosition(mark.Position, out span.iStartLine, out span.iStartIndex);
            view.Buffer.GetLineIndexOfPosition(mark.End, out span.iEndLine, out span.iEndIndex);

            Point startPoint = view.GetPointOfLineColumn(span.iStartLine, span.iStartIndex);
            if (startPoint == Point.Empty)
                return Rectangle.Empty;

            Point endPoint = view.GetPointOfLineColumn(span.iEndLine, span.iEndIndex);
            if (endPoint == Point.Empty)
                return Rectangle.Empty;

            int x = startPoint.X;
            int y = startPoint.Y;

            int height = endPoint.Y - y + view.LineHeight;
            int width = endPoint.X - x;

            return new Rectangle(x, y, width, height);
        }
    }
}
