using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight
{
    public static class IVsTextViewExtensions
    {
        public static IVsTextLines GetBuffer(this IVsTextView view)
        {
            IVsTextLines buffer;
            view.GetBuffer(out buffer);
            return buffer;
        }

        public static int GetLineHeight(this IVsTextView view)
        {
            int lineHeight;
            view.GetLineHeight(out lineHeight);
            return lineHeight;
        }

        public static Point GetPointOfLineColumn(this IVsTextView view, int line, int column)
        {
            var p = new Microsoft.VisualStudio.OLE.Interop.POINT[1];
            view.GetPointOfLineColumn(line, column, p);
            return new Point(p[0].x, p[0].y);
        }

        public static string GetSelectedText(this IVsTextView view)
        {
            string text;
            view.GetSelectedText(out text);

            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim();
            }

            return text;
        }
    }
}
