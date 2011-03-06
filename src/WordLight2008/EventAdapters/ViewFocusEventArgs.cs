using System;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.EventAdapters
{
	public class ViewFocusEventArgs : EventArgs
	{
		private IVsTextView _view;

		public IVsTextView View
		{
			get { return _view; }
		}

		public ViewFocusEventArgs(IVsTextView view)
		{
			_view = view;
		}
	}
}
