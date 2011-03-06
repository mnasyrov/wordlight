using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Extensions
{
    public static class IVsEnumHiddenRegionsExtensions
    {
        public static uint GetCount(this IVsEnumHiddenRegions enumHiddenRegions)
        {
            uint count;
            enumHiddenRegions.GetCount(out count);
            return count;
        }

        public static IList<TextSpan> GetHiddenRegions(this IVsEnumHiddenRegions enumHiddenRegions)
        {
            IList<TextSpan> regions = null;

            uint count = GetCount(enumHiddenRegions);

            if (count > 0)
            {
                regions = new List<TextSpan>((int)count);

                var hiddenRegions = new IVsHiddenRegion[count];

                enumHiddenRegions.Reset();
                enumHiddenRegions.Next(count, hiddenRegions, out count);

                for (int i = 0; i < count; i++)
                {
                    IVsHiddenRegion hiddenRegion = hiddenRegions[i];

                    uint state = 0;
                    hiddenRegion.GetState(out state);
                    if ((state & (uint)HIDDEN_REGION_STATE.hrsExpanded) == 0)
                    {
                        TextSpan[] pSpan = new TextSpan[1];
                        hiddenRegion.GetSpan(pSpan);
                        regions.Add(pSpan[0]);
                    }
                }
            }

            return regions;
        }
    }
}
