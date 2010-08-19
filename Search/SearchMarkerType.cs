using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
    [Guid(GuidConstants.SearchMarkerTypeString)]
    [ComVisible(true)]
    public class SearchMarkerType : IVsPackageDefinedTextMarkerType, IVsMergeableUIItem
	{
		#region IVsPackageDefinedTextMarkerType Members

		public int DrawGlyphWithColors(IntPtr hdc, Microsoft.VisualStudio.OLE.Interop.RECT[] pRect, int iMarkerType, IVsTextMarkerColorSet pMarkerColors, uint dwGlyphDrawFlags, int iLineHeight)
		{
			return VSConstants.E_NOTIMPL;
		}

		public int GetBehaviorFlags(out uint pdwFlags)
		{
            pdwFlags = (uint)(MARKERBEHAVIORFLAGS.MB_DEFAULT);
			return VSConstants.S_OK;
		}

		public int GetDefaultColors(COLORINDEX[] piForeground, COLORINDEX[] piBackground)
		{
            piForeground[0] = COLORINDEX.CI_USERTEXT_FG;
			piBackground[0] = COLORINDEX.CI_USERTEXT_BK;
			return VSConstants.S_OK;
		}

		public int GetDefaultFontFlags(out uint pdwFontFlags)
		{
			pdwFontFlags = (uint)FONTFLAGS.FF_DEFAULT;
			return VSConstants.S_OK;
		}

		public int GetDefaultLineStyle(COLORINDEX[] piLineColor, LINESTYLE[] piLineIndex)
		{
			piLineIndex[0] = LINESTYLE.LI_SOLID;
            piLineColor[0] = COLORINDEX.CI_MAGENTA;
			return VSConstants.S_OK;
		}

		public int GetPriorityIndex(out int piPriorityIndex)
		{
			piPriorityIndex = 200; // same as MARKERTYPE.MARKER_BOOKMARK;
			return VSConstants.S_OK;
		}

		public int GetVisualStyle(out uint pdwVisualFlags)
		{
			pdwVisualFlags = (uint)(MARKERVISUAL.MV_COLOR_ALWAYS | MARKERVISUAL.MV_BORDER);
			return VSConstants.S_OK;
		}

		#endregion

		#region IVsMergeableUIItem Members

		public int GetCanonicalName(out string pbstrNonLocalizeName)
		{
			pbstrNonLocalizeName = "WordLight Search Marker";
			return VSConstants.S_OK;
		}

		public int GetDescription(out string pbstrDesc)
		{
            pbstrDesc = "WordLight Search Marker";
			return VSConstants.S_OK;
		}

		public int GetDisplayName(out string pbstrDisplayName)
		{
            pbstrDisplayName = "WordLight Search Marker";
			return VSConstants.S_OK;
		}

		public int GetMergingPriority(out int piMergingPriority)
		{
			piMergingPriority = 0x2001;
			return VSConstants.S_OK;
		}

		#endregion
	}
}
