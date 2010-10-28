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
        private IDictionary<IntPtr, TextViewWindow> _textViews;
        private TextManagerEventAdapter _textManagerEvents;

        private TextViewWindow _currentView;
        private object _watcherSyncRoot = new object();

        public WindowWatcher(DTE2 application)
        {
            _textViews = new Dictionary<IntPtr, TextViewWindow>();

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
            lock (_watcherSyncRoot)
            {
                IntPtr windowHandle = e.View.GetWindowHandle();
                if (windowHandle != IntPtr.Zero && !_textViews.ContainsKey(windowHandle))
                {
                    var viewWindow = new TextViewWindow(e.View);
                    viewWindow.GotFocus += new EventHandler(ViewGotFocusHandler);
                    viewWindow.LostFocus += new EventHandler(ViewLostFocusHandler);

                    _currentView = viewWindow;

                    _textViews.Add(windowHandle, viewWindow);
                }
            }
        }

        private void ViewUnregisteredHandler(object sender, ViewRegistrationEventArgs e)
        {
            lock (_watcherSyncRoot)
            {
                IntPtr windowHandle = e.View.GetWindowHandle();
                if (_textViews.ContainsKey(windowHandle))
                {
                    TextViewWindow view = _textViews[windowHandle];
                    _textViews.Remove(windowHandle);

                    if (_currentView == view)
                        _currentView = null;

                    DisposeView(view);
                }
            }
        }

        public void Dispose()
        {
            lock (_watcherSyncRoot)
            {
                _currentView = null;

                foreach (TextViewWindow view in _textViews.Values)
                {
                    DisposeView(view);
                }
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
            lock (_watcherSyncRoot)
            {
                _currentView = view;
            }
        }

        private void ViewLostFocusHandler(object sender, EventArgs e)
        {
            var view = (TextViewWindow)sender;
            lock (_watcherSyncRoot)
            {
                if (_currentView == view)
                    _currentView = null;
            }
        }

        public TextViewWindow GetActiveTextView()
        {
            lock (_watcherSyncRoot)
            {
                return _currentView;
            }
        }
    }
}
