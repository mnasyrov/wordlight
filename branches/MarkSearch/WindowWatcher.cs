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
		private IDictionary<IntPtr, TextView> _textViews;
		private TextManagerEventAdapter _textManagerEvents;

		private TextViewWindow _currentWindow;
		private object _watcherSyncRoot = new object();

		public WindowWatcher(DTE2 application)
		{
			_textViews = new Dictionary<IntPtr, TextView>();

			IVsTextManager textManager = GetTextManager(application);

			_textManagerEvents = new TextManagerEventAdapter(textManager);
			_textManagerEvents.ViewRegistered += new EventHandler<ViewRegistrationEventArgs>(ViewRegisteredHandler);
			_textManagerEvents.ViewUnregistered += new EventHandler<ViewRegistrationEventArgs>(ViewUnregisteredHandler);
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

		private void ViewRegisteredHandler(object sender, ViewRegistrationEventArgs e)
		{
			try
			{
				lock (_watcherSyncRoot)
				{
					IntPtr windowHandle = e.View.GetWindowHandle();
					if (windowHandle != IntPtr.Zero && !_textViews.ContainsKey(windowHandle))
					{
						var textView = new TextView(e.View);

						textView.Window.GotFocus += new EventHandler(ViewGotFocusHandler);
						textView.Window.LostFocus += new EventHandler(ViewLostFocusHandler);

						_currentWindow = textView.Window;

						_textViews.Add(windowHandle, textView);

						Log.Debug("Registered view: {0}", windowHandle.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("Failed to register a view", ex);
			}
		}

		private void ViewUnregisteredHandler(object sender, ViewRegistrationEventArgs e)
		{
			try
			{
				lock (_watcherSyncRoot)
				{
					IntPtr windowHandle = e.View.GetWindowHandle();
					if (_textViews.ContainsKey(windowHandle))
					{
						TextView view = _textViews[windowHandle];
						_textViews.Remove(windowHandle);

						if (_currentWindow == view.Window)
							_currentWindow = null;

						DisposeView(view);

						Log.Debug("Unregistered view: {0}", windowHandle.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("Failed to unregister a view", ex);
			}
		}

		public void Dispose()
		{
			lock (_watcherSyncRoot)
			{
				_currentWindow = null;

				foreach (TextView view in _textViews.Values)
				{
					DisposeView(view);
				}

				_textViews.Clear();
			}
		}

		private void DisposeView(TextView view)
		{
			view.Window.GotFocus -= ViewGotFocusHandler;
			view.Window.LostFocus -= ViewLostFocusHandler;
			view.Dispose();
		}

		private void ViewGotFocusHandler(object sender, EventArgs e)
		{
			try
			{
				var view = (TextViewWindow)sender;
				lock (_watcherSyncRoot)
				{
					_currentWindow = view;
				}

				Log.Debug("View {0} got a focus", view.Handle.ToString());
			}
			catch (Exception ex)
			{
				Log.Error("Failed to process a view focus", ex);
			}
		}

		private void ViewLostFocusHandler(object sender, EventArgs e)
		{
			try
			{
				var view = (TextViewWindow)sender;
				lock (_watcherSyncRoot)
				{
					if (_currentWindow == view)
						_currentWindow = null;
				}

				Log.Debug("View {0} lost a focus", view.Handle.ToString());
			}
			catch (Exception ex)
			{
				Log.Error("Failed to process a lost view focus", ex);
			}
		}

		public TextViewWindow GetActiveTextWindow()
		{
			lock (_watcherSyncRoot)
			{
				return _currentWindow;
			}
		}
	}
}
