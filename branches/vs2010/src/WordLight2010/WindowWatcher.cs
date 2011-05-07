using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight;
using WordLight.EventAdapters;

namespace WordLight2010
{
	public class WindowWatcher : IDisposable
	{
		private IDictionary<IVsTextView, TextView> _textViews;
		private TextManagerEventAdapter _textManagerEvents;

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
            using (ServiceProvider wrapperSP = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)application))
            {
                return (IVsTextManager)wrapperSP.GetService(typeof(SVsTextManager));
            }
		}

		private void ViewRegisteredHandler(object sender, ViewRegistrationEventArgs e)
		{
			try
			{
                //System.Threading.ThreadPool.QueueUserWorkItem((object state) =>
                //{
					try
					{
						System.Threading.Thread.Sleep(200);
						lock (_watcherSyncRoot)
						{
							if (e.View != null && !_textViews.ContainsKey(e.View))
							{
								var textView = new TextView(e.View);
								_textViews.Add(e.View, textView);

								Log.Debug("Registered view: {0}", e.View.GetHashCode());
							}
						}
					}
					catch (Exception ex)
					{
						Log.Error("Failed to register a view", ex);
					}
				//});
			}
			catch (Exception ex)
			{
				Log.Error("Failed to enqueue a work item", ex);
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

						view.Dispose();

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
				foreach (TextView view in _textViews.Values)
				{
					view.Dispose();
				}

				_textViews.Clear();
			}
		}

		public TextView GetActiveTextView()
		{
			lock (_watcherSyncRoot)
			{
                IVsTextView activeVsView;
                _textManager.GetActiveView(Convert.ToInt32(true), null, out activeVsView);

				if (activeVsView != null && _textViews.ContainsKey(activeVsView))
					return _textViews[activeVsView];
				else
					return null;
			}
		}
	}
}
