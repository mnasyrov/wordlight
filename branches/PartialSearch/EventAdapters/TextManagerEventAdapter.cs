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
				// Excellent comment from MetalScroll addin. Can't say better.
				// (http://code.google.com/p/metalscroll/source/browse/trunk/Connect.cpp)

				// Unfortunately, the window hasn't been created at this point yet, so we can't get the HWND
				// here. Register an even handler to catch SetFocus(), and get the HWND from there. We'll remove
				// the handler after the first SetFocus() as we don't care about getting more events once we
				// have the HWND.

				var textViewEvents = new TextViewEventAdapter(view);
				textViewEvents.SetFocus += new EventHandler<ViewFocusEventArgs>(textViewEvents_SetFocus);
			}
		}

		private void textViewEvents_SetFocus(object sender, ViewFocusEventArgs e)
		{
			IVsTextView view = e.View;

			var textViewEvents = (TextViewEventAdapter)sender;
			textViewEvents.SetFocus -= textViewEvents_SetFocus;
			textViewEvents.Dispose();

			EventHandler<ViewRegistrationEventArgs> evt = ViewRegistered;
			if (evt != null) evt(this, new ViewRegistrationEventArgs(view));
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
