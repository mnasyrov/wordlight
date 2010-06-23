using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight
{
    public class WindowWatcher : IDisposable
    {
        private DTE2 _applicationObject;
        private WindowEvents _windowEvents;
        private IntPtr _mainWindowHandle;
        private IVsTextManager _textManager;
        private Dictionary<string, TextPaneWindow> _textPaneWindows;

		public event EventHandler OnTextPaneInput;

        [DllImport("User32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string windowName);

        public WindowWatcher(DTE2 application)
        {
            _applicationObject = application;
            _mainWindowHandle = (IntPtr)application.MainWindow.HWnd;

            _textPaneWindows = new Dictionary<string, TextPaneWindow>();

            // Get IVsTextManager
            var serviceProvider = _applicationObject as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

            Guid SID = typeof(SVsTextManager).GUID;
            Guid IID = typeof(IVsTextManager).GUID;
            IntPtr output;
            serviceProvider.QueryService(ref SID, ref IID, out output);

            _textManager = (IVsTextManager)Marshal.GetObjectForIUnknown(output);

            // Catch window events
            _windowEvents = _applicationObject.Events.get_WindowEvents(null);

            _windowEvents.WindowCreated += new _dispWindowEvents_WindowCreatedEventHandler(OnWindowCreated);
            _windowEvents.WindowClosing += new _dispWindowEvents_WindowClosingEventHandler(OnWindowClosing);
        }

        private void OnWindowCreated(Window win)
        {
            TextWindow textWin = win.Object as TextWindow;
            if (textWin != null)
            {
                AddTextPane(textWin.ActivePane);
            }
        }

        private TextPaneWindow AddTextPane(TextPane pane)
        {
            TextPaneWindow paneWindow = null;

            if (pane != null)
            {
                IntPtr paneHandle = GetVsTextEditPane(_mainWindowHandle, pane.Window);
                if (paneHandle != IntPtr.Zero)
                {
                    IVsTextView view;
                    _textManager.GetActiveView(1, null, out view);

                    paneWindow = new TextPaneWindow(paneHandle, pane, view);
					paneWindow.OnInput += new EventHandler(paneWindow_OnInput);

                    _textPaneWindows.Add(pane.Window.Caption, paneWindow);
                }
            }

            return paneWindow;
        }

        private void OnWindowClosing(Window Window)
        {
            TextWindow textWin = Window.Object as TextWindow;
            if (textWin != null)
            {
                TextPane pane = textWin.ActivePane;
                if (_textPaneWindows.ContainsKey(pane.Window.Caption))
                {
                    TextPaneWindow paneWindow = _textPaneWindows[pane.Window.Caption];
                    _textPaneWindows.Remove(pane.Window.Caption);

					paneWindow.OnInput -= paneWindow_OnInput;
                    paneWindow.ReleaseHandle();
                }
            }
        }

        public TextPaneWindow GetPaneWindow(TextPane pane)
        {
            TextPaneWindow paneWindow = null;

            if (pane != null)
            {
                if (_textPaneWindows.ContainsKey(pane.Window.Caption))
                {
                    paneWindow = _textPaneWindows[pane.Window.Caption];
                }
            }

            return paneWindow;
        }

        private static IntPtr GetVsTextEditPane(IntPtr _mainWindowHandle, Window wnd)
        {
            IntPtr pane = IntPtr.Zero;

            if (wnd != null)
            {
                string caption = wnd.Caption;

                IntPtr hwndParent = FindWindowEx(_mainWindowHandle, IntPtr.Zero, null, caption); // top level that we can id
                // This is hackish since we have no easier way to do this
                hwndParent = FindWindowEx(hwndParent, IntPtr.Zero, null, null);  
                hwndParent = FindWindowEx(hwndParent, IntPtr.Zero, null, null); // an intermediate window
                pane = FindWindowEx(hwndParent, IntPtr.Zero, null, null); // This is the VsTextEditPane item
            }

            return pane;
        }

        public void Dispose()
        {
            if (_windowEvents != null)
            {
                _windowEvents.WindowCreated -= new _dispWindowEvents_WindowCreatedEventHandler(OnWindowCreated);
                _windowEvents.WindowClosing -= new _dispWindowEvents_WindowClosingEventHandler(OnWindowClosing);
            }

            foreach (TextPaneWindow win in _textPaneWindows.Values)
            {
                win.ReleaseHandle();
            }
        }

		private void paneWindow_OnInput(object sender, EventArgs e)
		{
			EventHandler evt = OnTextPaneInput;
			if (evt != null) evt(this, EventArgs.Empty);
		}
    }
}
