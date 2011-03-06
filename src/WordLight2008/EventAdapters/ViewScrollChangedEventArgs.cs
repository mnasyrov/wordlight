using System;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.EventAdapters
{
	public class ViewScrollChangedEventArgs : EventArgs
	{
		private IVsTextView _view;
		private ViewScrollInfo _scrollInfo;

		public IVsTextView View
		{
			get { return _view; }
		}

		public ViewScrollInfo ScrollInfo
		{
			get { return _scrollInfo; }
		}

		public ViewScrollChangedEventArgs(IVsTextView view, ViewScrollInfo scrollInfo)
		{
			_view = view;
			_scrollInfo = scrollInfo;
		}
	}
}
