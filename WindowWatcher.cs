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
		private IDictionary<IVsTextView, TextView> _textViews;
		private TextManagerEventAdapter _textManagerEvents;

		//private TextView _activeTextView;
		private object _watcherSyncRoot = new object();

        private DTE2 _application;
        private IVsTextManager _textManager;

		public WindowWatcher(DTE2 application)
		{
            if (application == null) throw new ArgumentNullException("application");

            _application = application;

            _textViews = new Dictionary<IVsTextView, TextView>();

			_textManager = GetTextManager(application);

            _textManagerEvents = new TextManagerEventAdapter(_textManager);
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
                    if (e.View != null && !_textViews.ContainsKey(e.View))
					{
						var textView = new TextView(e.View);

                        //textView.GotFocus += new EventHandler(ViewGotFocusHandler);
                        //textView.LostFocus += new EventHandler(ViewLostFocusHandler);

						//_activeTextView = textView;

                        _textViews.Add(e.View, textView);

                        Log.Debug("Registered view: {0}", e.View.GetHashCode());
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
					if (e.View != null && _textViews.ContainsKey(e.View))
					{
                        TextView view = _textViews[e.View];
                        _textViews.Remove(e.View);

                        //if (_activeTextView == view)
                        //    _activeTextView = null;

						DisposeView(view);

                        Log.Debug("Unregistered view: {0}", e.View.GetHashCode().ToString());
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
				//_activeTextView = null;

				foreach (TextView view in _textViews.Values)
				{
					DisposeView(view);
				}

				_textViews.Clear();
			}
		}

		private void DisposeView(TextView view)
		{
            //view.GotFocus -= ViewGotFocusHandler;
            //view.LostFocus -= ViewLostFocusHandler;
			view.Dispose();
		}

        //private void ViewGotFocusHandler(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var view = (TextView)sender;
        //        lock (_watcherSyncRoot)
        //        {
        //            _activeTextView = view;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error("Failed to process a view focus", ex);
        //    }
        //}

        //private void ViewLostFocusHandler(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var view = (TextView)sender;
        //        lock (_watcherSyncRoot)
        //        {
        //            if (_activeTextView == view)
        //                _activeTextView = null;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error("Failed to process a lost view focus", ex);
        //    }
        //}

		public TextView GetActiveTextView()
		{
			lock (_watcherSyncRoot)
			{
                IVsTextView activeVsView;
                _textManager.GetActiveView(Convert.ToInt32(true), null, out activeVsView);

                lock (_watcherSyncRoot)
                {
                    if (activeVsView == null || !_textViews.ContainsKey(activeVsView))
                        return null;
                    else
                        return _textViews[activeVsView];
                }
			}
		}
	}
}
