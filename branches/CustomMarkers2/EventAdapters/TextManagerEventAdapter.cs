using System;
using System.Collections.Generic;
using System.Text;

using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.EventAdapters
{
    public class TextManagerEventAdapter : IVsTextManagerEvents, IDisposable
    {
        private uint _connectionCookie;
        private IConnectionPoint _connectionPoint;

        /// <summary>
        /// Fires when a view is registered.  
        /// </summary>
        public event EventHandler<ViewRegistrationEventArgs> ViewRegistered;

        /// <summary>
        /// Fires when a view is unregistered.  
        /// </summary>
        public event EventHandler<ViewRegistrationEventArgs> ViewUnregistered;

        public TextManagerEventAdapter(IVsTextManager textManager)
        {
            if (textManager == null) throw new ArgumentNullException("textManager");

            var cpContainer = textManager as IConnectionPointContainer;
            Guid riid = typeof(IVsTextManagerEvents).GUID;
            cpContainer.FindConnectionPoint(ref riid, out _connectionPoint);

            _connectionPoint.Advise(this, out _connectionCookie);
        }

        public void Dispose()
        {
            if (_connectionCookie > 0)
            {
                _connectionPoint.Unadvise(_connectionCookie);
                _connectionCookie = 0;
            }
        }

        public void OnRegisterView(IVsTextView view)
        {
            if (view != null)
            {
                EventHandler<ViewRegistrationEventArgs> evt = ViewRegistered;
                if (evt != null) evt(this, new ViewRegistrationEventArgs(view));
            }
        }

		public void OnUnregisterView(IVsTextView view)
        {
            if (view != null)
            {
                EventHandler<ViewRegistrationEventArgs> evt = ViewUnregistered;
                if (evt != null) evt(this, new ViewRegistrationEventArgs(view));
            }
        }

        #region Unused events

        public void OnRegisterMarkerType(int iMarkerType)
        {
            // Do nothing
        }

        public void OnUserPreferencesChanged(
            VIEWPREFERENCES[] pViewPrefs,
            FRAMEPREFERENCES[] pFramePrefs,
            LANGPREFERENCES[] pLangPrefs,
            FONTCOLORPREFERENCES[] pColorPrefs)
        {
            // Do nothing
        }

        #endregion
    }
}
