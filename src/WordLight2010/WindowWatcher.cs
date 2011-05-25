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
	public class WindowWatcher
	{
		IVsEditorAdaptersFactoryService _editorAdapterService;
        private IVsTextManager _textManager;

		public WindowWatcher(IServiceProvider serviceProvider)
		{
			if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");

			_textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));

			var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
			_editorAdapterService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
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
			IVsTextView activeVsView;

			if (Microsoft.VisualStudio.ErrorHandler.Succeeded(
				_textManager.GetActiveView(Convert.ToInt32(true), null, out activeVsView)
			))
			{
				if (activeVsView != null)
				{
					var wpfTextView = _editorAdapterService.GetWpfTextView(activeVsView);
					return MarkAdornmentFactory.FindMarkAdorment(wpfTextView);
				}
			}

			return null;
		}
	}
}
