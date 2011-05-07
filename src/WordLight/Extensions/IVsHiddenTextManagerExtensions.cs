using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight
{
    public static class IVsHiddenTextManagerExtensions
    {
        public static IVsHiddenTextSession GetHiddenTextSession(
            this IVsHiddenTextManager hiddenTextManager, IVsTextLines buffer)
        {
            IVsHiddenTextSession hiddenTextSession;
            hiddenTextManager.GetHiddenTextSession(buffer, out hiddenTextSession);
            return hiddenTextSession;
        }
    }
}
