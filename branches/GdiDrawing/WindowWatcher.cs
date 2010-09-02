using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using EnvDTE80;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.EventAdapters;

namespace WordLight
{
    public class WindowWatcher : IDisposable
    {
        private IVsTextManager _textManager;
        private IDictionary<IntPtr, TextViewWindow> _textViews;
		private TextManagerEventAdapter _textManagerEvents;

		private TextViewWindow _currentView;
		private object _currentViewSyncRoot = new object();

        public WindowWatcher(DTE2 application)
        {
			_textViews = new Dictionary<IntPtr, TextViewWindow>();

			_textManager = GetTextManager(application);
			_textManagerEvents = new TextManagerEventAdapter(_textManager);

			_textManagerEvents.ViewRegistered += 
				new EventHandler<ViewRegistrationEventArgs>(_textManagerEvents_ViewRegistered);
			_textManagerEvents.ViewUnregistered += 
				new EventHandler<ViewRegistrationEventArgs>(_textManagerEvents_ViewUnregistered);
        }

		private IVsTextManager GetTextManager(DTE2 application)
		{
			var serviceProvider = application as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

			Guid SID = typeof(SVsTextManager).GUID;
			Guid IID = typeof(IVsTextManager).GUID;
			IntPtr output;
			serviceProvider.QueryService(ref SID, ref IID, out output);

			return (IVsTextManager)Marshal.GetObjectForIUnknown(output);
		}		

		private void _textManagerEvents_ViewRegistered(object sender, ViewRegistrationEventArgs e)
		{
			IntPtr windowHandle = e.View.GetWindowHandle();
			if (windowHandle != IntPtr.Zero && !_textViews.ContainsKey(windowHandle))
			{
                TextViewWindow view = new TextViewWindow(e.View, (IVsHiddenTextManager)_textManager);
				_textViews.Add(windowHandle, view);
				view.GotFocus += new EventHandler(ViewGotFocusHandler);
				view.LostFocus += new EventHandler(ViewLostFocusHandler);

				lock (_currentViewSyncRoot)
				{
					if (_textViews.Count == 1)
					{
						_currentView = view;
					}
				}
			}
		}

		private void _textManagerEvents_ViewUnregistered(object sender, ViewRegistrationEventArgs e)
		{
			IntPtr windowHandle = e.View.GetWindowHandle();
			if (_textViews.ContainsKey(windowHandle))
			{
				TextViewWindow view = _textViews[windowHandle];
				_textViews.Remove(windowHandle);

				lock (_currentViewSyncRoot)
				{
					if (_currentView == view)
						_currentView = null;
				}

				DisposeView(view);
			}
		}

        public void Dispose()
        {
			lock (_currentViewSyncRoot)
			{
				_currentView = null;
			}

            foreach (TextViewWindow view in _textViews.Values)
            {
				DisposeView(view);
            }
        }

		private void DisposeView(TextViewWindow view)
		{
			view.GotFocus -= ViewGotFocusHandler;
			view.LostFocus -= ViewLostFocusHandler;
			view.Dispose();
		}

		private void ViewGotFocusHandler(object sender, EventArgs e)
		{
			var view = (TextViewWindow)sender;
			lock (_currentViewSyncRoot)
			{
				_currentView = view;
			}
		}

		private void ViewLostFocusHandler(object sender, EventArgs e)
		{
			var view = (TextViewWindow)sender;
			lock (_currentViewSyncRoot)
			{
				//if (_currentView == view)
					//_currentView = null;
			}
		}

		public void FreezeSearch(int searchGroup)
		{
			lock (_currentViewSyncRoot)
			{
				if (_currentView != null)
					_currentView.FreezeSearch(searchGroup);
			}
		}
    }
}
