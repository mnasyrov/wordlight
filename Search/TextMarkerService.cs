using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
    [Guid(GuidConstants.TextMarkerServiceString)]
    [ComVisible(true)]
    public class TextMarkerService :
        Microsoft.VisualStudio.OLE.Interop.IServiceProvider, 
        IVsTextMarkerTypeProvider
    {
        #region IServiceProvider Members

        public int QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            ppvObject = IntPtr.Zero;
            if (guidService == GuidConstants.TextMarkerService && riid == typeof(IVsTextMarkerTypeProvider).GUID)
            {
                IntPtr tmp = Marshal.GetIUnknownForObject(this);
                return Marshal.QueryInterface(tmp, ref riid, out ppvObject);
            }
            return VSConstants.E_NOINTERFACE;
        }

        #endregion


        #region IVsTextMarkerTypeProvider Members

        public int GetTextMarkerType(ref Guid pguidMarker, out IVsPackageDefinedTextMarkerType ppMarkerType)
        {
            if (pguidMarker == GuidConstants.SearchMarkerType)
            {
                ppMarkerType = new SearchMarkerType();
                return VSConstants.S_OK;
            }
            else
            {
                ppMarkerType = null;
                return VSConstants.E_UNEXPECTED;
            }
        }

        #endregion
    }
}
