using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Extensions
{
    public static class IVsHiddenTextSessionExtensions
    {
        public static IList<TextSpan> GetAllHiddenRegions(this IVsHiddenTextSession hiddenTextSession, TextSpan searchRange)
        {
            var pSearchRange = new TextSpan[1];
            pSearchRange[0] = searchRange;

            IVsEnumHiddenRegions enumHiddenRegions;

            hiddenTextSession.EnumHiddenRegions(
                (uint)FIND_HIDDEN_REGION_FLAGS.FHR_ALL_REGIONS,
                0, // dwCookie for custom regions, not used here
                pSearchRange,
                out enumHiddenRegions
            );

            if (enumHiddenRegions != null)
            {
                 return enumHiddenRegions.GetHiddenRegions();
            }

            return null;
        }
    }
}
