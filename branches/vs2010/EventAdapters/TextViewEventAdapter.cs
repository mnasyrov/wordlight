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
		private uint _connectionCookie = 0;
        private IVsTextView _view;        

		public TextViewEventAdapter(IVsTextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

            _view = view;

            try
            {
                var connectionPoint = FindConnectionPoint(_view);
                connectionPoint.Advise(this, out _connectionCookie);

                Log.Debug("_connectionCookie: {0}", _connectionCookie);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to create TextViewEventAdapter", ex);
            }
		}

		public void Dispose()
		{
            Log.Debug("Disposing TextViewEvent...");

			if (_connectionCookie > 0)
			{
                var connectionPoint = FindConnectionPoint(_view);

				connectionPoint.Unadvise(_connectionCookie);
                Log.Debug("Unadvised connectionCookie: {0}", _connectionCookie);

				_connectionCookie = 0;
			}
		}

		/// <summary>
		/// Fires when a view receives focus.
		/// </summary>
		public event EventHandler<ViewFocusEventArgs> GotFocus;
		public event EventHandler<ViewFocusEventArgs> LostFocus;
		public event EventHandler<ViewScrollChangedEventArgs> ScrollChanged;

		public void OnSetFocus(IVsTextView view)
		{
			var evt = GotFocus;
			if (evt != null && view != null) evt(this, new ViewFocusEventArgs(view));
		}

		public void OnKillFocus(IVsTextView view)
		{
			var evt = LostFocus;
			if (evt != null && view != null) evt(this, new ViewFocusEventArgs(view));
		}

		public void OnChangeScrollInfo(IVsTextView view, int iBar, int iMinUnit, int iMaxUnits, int iVisibleUnits, int iFirstVisibleUnit)
		{
			var evt = ScrollChanged;
			if (evt != null)
			{
				ViewScrollInfo scrollInfo = new ViewScrollInfo()
				{
					bar = iBar,
					minUnit = iMinUnit,
					maxUnit = iMaxUnits,
					visibleUnits = iVisibleUnits,
					firstVisibleUnit = iFirstVisibleUnit
				};
				evt(this, new ViewScrollChangedEventArgs(view, scrollInfo));
			}
		}

		#region Unused events

		public void OnChangeCaretLine(IVsTextView pView, int iNewLine, int iOldLine)
		{
			// Do nothing
		}		

		public void OnSetBuffer(IVsTextView pView, IVsTextLines pBuffer)
		{
			// Do nothing
		}

		#endregion

        private static IConnectionPoint FindConnectionPoint(IVsTextView view)
        {
            var cpContainer = view as IConnectionPointContainer;

            Guid riid = typeof(IVsTextViewEvents).GUID;
            IConnectionPoint connectionPoint;

            cpContainer.FindConnectionPoint(ref riid, out connectionPoint);

            return connectionPoint;
        }
	}
}
