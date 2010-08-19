﻿using System;
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
        private IDictionary<IntPtr, TextViewWindow> _viewWindows;
		private TextManagerEventAdapter _textManagerEvents;

        public WindowWatcher(DTE2 application)
        {
			_viewWindows = new Dictionary<IntPtr, TextViewWindow>();

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
			if (windowHandle != IntPtr.Zero && !_viewWindows.ContainsKey(windowHandle))
			{
                TextViewWindow viewWindow = new TextViewWindow(e.View, (IVsHiddenTextManager)_textManager);
				_viewWindows.Add(windowHandle, viewWindow);
			}
		}

		private void _textManagerEvents_ViewUnregistered(object sender, ViewRegistrationEventArgs e)
		{
			IntPtr windowHandle = e.View.GetWindowHandle();
			if (_viewWindows.ContainsKey(windowHandle))
			{
				TextViewWindow paneWindow = _viewWindows[windowHandle];
				_viewWindows.Remove(windowHandle);
				paneWindow.Dispose();
			}
		}

        public void Dispose()
        {
            foreach (TextViewWindow win in _viewWindows.Values)
            {
				win.Dispose();
            }
        }
    }
}