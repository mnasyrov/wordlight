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
	public class TextViewEventAdapter : IVsTextViewEvents, IDisposable
	{
		private uint _connectionCookie;
		private IConnectionPoint _connectionPoint;

		public TextViewEventAdapter(IVsTextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

			var cpContainer = view as IConnectionPointContainer;
			Guid riid = typeof(IVsTextViewEvents).GUID;
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

		/// <summary>
		/// Fires when a view receives focus.
		/// </summary>
		public event EventHandler<ViewFocusEventArgs> SetFocus;

		public void OnSetFocus(IVsTextView view)
		{
			EventHandler<ViewFocusEventArgs> evt = SetFocus;
			if (evt != null) evt(this, new ViewFocusEventArgs(view));
		}

		#region Unused events

		public void OnChangeCaretLine(IVsTextView pView, int iNewLine, int iOldLine)
		{
			// Do nothing
		}

		public void OnChangeScrollInfo(IVsTextView pView, int iBar, int iMinUnit, int iMaxUnits, int iVisibleUnits, int iFirstVisibleUnit)
		{
			// Do nothing
		}

		public void OnKillFocus(IVsTextView pView)
		{
			// Do nothing
		}

		public void OnSetBuffer(IVsTextView pView, IVsTextLines pBuffer)
		{
			// Do nothing
		}


		#endregion
	}
}
