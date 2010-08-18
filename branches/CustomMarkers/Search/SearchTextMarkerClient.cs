using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using WordLight.Extensions;

namespace WordLight.Search
{
    public class SearchTextMarkerClient : IVsTextMarkerClient
    {
        // Summary:
        //     Executes a command on a specific marker within the text buffer.
        //
        // Parameters:
        //   pMarker:
        //     [in] Pointer to the Microsoft.VisualStudio.TextManager.Interop.IVsTextMarker
        //     interface for the marker.
        //
        //   iItem:
        //     [in] Command selected by the user from the context menu. For a list of iItem
        //     values, see Microsoft.VisualStudio.TextManager.Interop.MarkerCommandValues.
        //
        // Returns:
        //     If the method succeeds, it returns Microsoft.VisualStudio.VSConstants.S_OK.
        //     If it fails, it returns an error code.
        public int ExecMarkerCommand(IVsTextMarker pMarker, int iItem)
        {
            // Not implemented
            return VSConstants.E_NOTIMPL;
        }
        
        //
        // Summary:
        //     Queries the marker for the command information.
        //
        // Parameters:
        //   pMarker:
        //     [in] Pointer to the Microsoft.VisualStudio.TextManager.Interop.IVsTextMarker
        //     interface for the marker.
        //
        //   iItem:
        //     [in] ] Command selected by the user from the context menu. For a list of
        //     iItem values, see Microsoft.VisualStudio.TextManager.Interop.MarkerCommandValues.
        //
        //   pbstrText:
        //     [out] Text of the marker command in the context menu.
        //
        //   pcmdf:
        //     [out] Pointer to command flags.
        //
        // Returns:
        //     If the method succeeds, it returns Microsoft.VisualStudio.VSConstants.S_OK.
        //     If it fails, it returns an error code.
        public int GetMarkerCommandInfo(IVsTextMarker pMarker, int iItem, string[] pbstrText, uint[] pcmdf)
        {
            // Not implemented
            return VSConstants.E_NOTIMPL;
        }

        //
        // Summary:
        //     Returns the tip text for the text marker when the mouse hovers over the marker.
        //
        // Parameters:
        //   pMarker:
        //     [in] Pointer to the Microsoft.VisualStudio.TextManager.Interop.IVsTextMarker
        //     interface for the marker.
        //
        //   pbstrText:
        //     [out] Tip text associated with the marker.
        //
        // Returns:
        //     If the method succeeds, it returns Microsoft.VisualStudio.VSConstants.S_OK.
        //     If it fails, it returns an error code.
        public int GetTipText(IVsTextMarker pMarker, string[] pbstrText)
        {
            // Not implemented
            return VSConstants.E_NOTIMPL;
        }

        //
        // Summary:
        //     Called when the text associated with a marker is deleted by a user action.
        public void MarkerInvalidated()
        {
            // Not implemented
            //return VSConstants.E_NOTIMPL;
        }

        //
        // Summary:
        //     Signals that the marker position has changed.
        //
        // Parameters:
        //   pMarker:
        //     [in] Pointer to the Microsoft.VisualStudio.TextManager.Interop.IVsTextMarker
        //     interface for the marker that was changed.
        //
        // Returns:
        //     If the method succeeds, it returns Microsoft.VisualStudio.VSConstants.S_OK.
        //     If it fails, it returns an error code.
        public int OnAfterMarkerChange(IVsTextMarker pMarker)
        {
            // Not implemented
            return VSConstants.E_NOTIMPL;
        }

        //
        // Summary:
        //     Signals that the text under the marker has been altered but the marker has
        //     not been deleted.
        public void OnAfterSpanReload()
        {
            // Not implemented
        }

        //
        // Summary:
        //     Sends notification that the text buffer is about to close.
        public void OnBeforeBufferClose()
        {
            // Not implemented
        }
        
        //
        // Summary:
        //     Determines whether the buffer was saved to a different name.
        //
        // Parameters:
        //   pszFileName:
        //     [in] File name associated with the text buffer. Can be null in buffers where
        //     the file name cannot change.
        public void OnBufferSave(string pszFileName)
        {
            // Not implemented
        }
    }
}
