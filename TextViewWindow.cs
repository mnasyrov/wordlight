using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.NativeMethods;
using WordLight.EventAdapters;
using WordLight.Extensions;
using WordLight.Search;

namespace WordLight
{
	public class TextViewWindow : NativeWindow, IDisposable
	{
		private TextView _view;
		private string _previousSelectedText;
		
		private object _paintSync = new object();

        public event PaintEventHandler Paint;
        public event EventHandler PaintEnd;

		public TextView View
		{
			get { return _view; }
		}

		public TextViewWindow(TextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

			_view = view;			
			AssignHandle(_view.WindowHandle);

            Log.Debug("TextViewWindow is created");
		}

		public void Dispose()
		{			
			ReleaseHandle();
		}

		protected override void WndProc(ref Message m)
		{
            try
            {
			    switch (m.Msg)
			    {
				    case WinProcMessages.WM_KEYUP:
				    case WinProcMessages.WM_KEYDOWN:
				    case WinProcMessages.WM_LBUTTONUP:
				    case WinProcMessages.WM_RBUTTONUP:
				    case WinProcMessages.WM_MBUTTONUP:
				    case WinProcMessages.WM_XBUTTONUP:
				    case WinProcMessages.WM_LBUTTONDOWN:
				    case WinProcMessages.WM_MBUTTONDOWN:
				    case WinProcMessages.WM_RBUTTONDOWN:
				    case WinProcMessages.WM_XBUTTONDOWN:
				    case WinProcMessages.WM_LBUTTONDBLCLK:
				    case WinProcMessages.WM_MBUTTONDBLCLK:
				    case WinProcMessages.WM_RBUTTONDBLCLK:
				    case WinProcMessages.WM_XBUTTONDBLCLK:
					    base.WndProc(ref m);
					    HandleUserInput();
					    break;

				    case WinProcMessages.WM_PAINT:
					    Rectangle clipRect = User32.GetUpdateRect(Handle, false).ToRectangle();

					    base.WndProc(ref m);

					    if (clipRect != Rectangle.Empty)
					    {
						    OnPaint(clipRect);
					    }

					    break;

				    default:
					    base.WndProc(ref m);
					    break;
			    }
	    	}
            catch (Exception ex)
            {
                Log.Error("Unhandled exception during processing window messages", ex);
            }
		}

		private void HandleUserInput()
		{
			string text = _view.GetSelectedText();

			if (text != _previousSelectedText)
			{
				_previousSelectedText = text;
				_view.SearchText(text);
			}
		}

		private void OnPaint(Rectangle clipRect)
		{
			Monitor.Enter(_paintSync);

			User32.HideCaret(Handle);

			using (Graphics g = Graphics.FromHwnd(Handle))
			{
                if (clipRect == Rectangle.Empty)
                {
                    clipRect = Rectangle.Truncate(g.VisibleClipBounds);
                }

                g.SetClip(clipRect);

                var evt = Paint;
                if (evt != null) evt(this, new PaintEventArgs(g, clipRect));
			}

			User32.ShowCaret(Handle);

            var paintEndEvent = PaintEnd;
            if (paintEndEvent != null) paintEndEvent(this, EventArgs.Empty);

			Monitor.Exit(_paintSync);
		}		
	}
}
