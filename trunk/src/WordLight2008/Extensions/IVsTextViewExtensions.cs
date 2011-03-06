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
