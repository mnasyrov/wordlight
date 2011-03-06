using System;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.EventAdapters
{
	public class ViewRegistrationEventArgs : EventArgs
	{
		private IVsTextView _view;

		public IVsTextView View
		{
			get { return _view; }
		}

		public ViewRegistrationEventArgs(IVsTextView view)
		{
			_view = view;
		}
	}
}
