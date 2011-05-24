using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;

using EnvDTE80;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight;
using WordLight.EventAdapters;

namespace WordLight2010
{
	public class WindowWatcher : IDisposable
	{
		private IDictionary<IVsTextView, MarkAdornment> _adornments;
		private TextManagerEventAdapter _textManagerEvents;
		IVsEditorAdaptersFactoryService _editorAdapterService;

		private object _watcherSyncRoot = new object();

        private IVsTextManager _textManager;

		public WindowWatcher(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");
			
			_adornments = new Dictionary<IVsTextView, MarkAdornment>();

			IComponentModel componentModel;

			//using (ServiceProvider wrapperSP = 
			//    new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)application))
			//{
			//    _textManager = (IVsTextManager)wrapperSP.GetService(typeof(SVsTextManager));
			//    componentModel = (IComponentModel)wrapperSP.GetService(typeof(SComponentModel));
			//}

			_textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));
			componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));

			_editorAdapterService = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            _textManagerEvents = new TextManagerEventAdapter(_textManager);
			_textManagerEvents.ViewRegistered += new EventHandler<ViewRegistrationEventArgs>(ViewRegisteredHandler);
			_textManagerEvents.ViewUnregistered += new EventHandler<ViewRegistrationEventArgs>(ViewUnregisteredHandler);
		}

		private void ViewRegisteredHandler(object sender, ViewRegistrationEventArgs e)
		{
			try
			{
				//System.Threading.ThreadPool.QueueUserWorkItem((object state) =>
				//{
					try
					{
						//System.Threading.Thread.Sleep(200);
						lock (_watcherSyncRoot)
						{
							if (e.View != null && !_adornments.ContainsKey(e.View))
							{
								var wpfTextView = _editorAdapterService.GetWpfTextView(e.View);
								var adornment = new MarkAdornment(wpfTextView);

								_adornments.Add(e.View, adornment);

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
					if (e.View != null && _adornments.ContainsKey(e.View))
					{
					    var adornment = _adornments[e.View];
					    _adornments.Remove(e.View);

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
				_adornments.Clear();
			}
		}

		public void FreezeSearchOnActiveTextView(int groupIndex)
		{
			var activeAdornment = GetActiveMarkAdornment();

			if (activeAdornment != null)
			{
				activeAdornment.FreezeSearch(groupIndex);

				Log.Debug("Freezed search group: {0}", groupIndex);
			}
			else
			{
				Log.Debug("No active view to freeze group: {0}", groupIndex);
			}
		}

		private MarkAdornment GetActiveMarkAdornment()
		{
			lock (_watcherSyncRoot)
			{
				IVsTextView activeVsView;
				_textManager.GetActiveView(Convert.ToInt32(true), null, out activeVsView);

				if (activeVsView != null && _adornments.ContainsKey(activeVsView))
					return _adornments[activeVsView];
				else
					return null;
			}
		}
	}
}
