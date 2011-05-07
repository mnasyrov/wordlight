using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Controls;

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

using WordLight;
using WordLight.NativeMethods;
using WordLight.EventAdapters;
using WordLight.Extensions;
using WordLight.Search;

namespace WordLight2010
{
	/*[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	public sealed class TextAdornment1Factory : IWpfTextViewCreationListener
	{
		[Export(typeof(AdornmentLayerDefinition))]
		[Name("ExperimentalAdornment")]
		//[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
		[TextViewRole(PredefinedTextViewRoles.Document)]
		public AdornmentLayerDefinition editorAdornmentLayer = null;

		public void TextViewCreated(IWpfTextView textView)
		{
		}
	}

	public class ExperimentalAdornment
	{		
		IAdornmentLayer _layer;
		IWpfTextView _view;
		int _lineNumber = 0;
		public ExperimentalAdornment(IWpfTextView view)
		{
			_layer = view.GetAdornmentLayer("ExperimentalAdornment");
			_view = view;

			_lineNumber = view.Selection.Start.Position.GetContainingLine().LineNumber;
			_view.LayoutChanged += OnLayoutChanged;
		}

		private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{			
			var line = _view.TextSnapshot.GetLineFromLineNumber(_lineNumber);
			if (line != null)
			{
				FormatLine(line.Extent);
			}
		}

		private void FormatLine(SnapshotSpan span)
		{
			var g = _view.TextViewLines.GetMarkerGeometry(span);
			if (g != null)
			{
				//var textblock = new TextBlock();
				//textblock.Text = span.GetText();
				//textblock.FontFamily = new FontFamily("Consolas");
				//textblock.FontWeight = FontWeights.ExtraBold;

				var rectangle = new System.Windows.Shapes.Rectangle();
				rectangle.Width = g.Bounds.Width;
				rectangle.Height = g.Bounds.Height;
				rectangle.Fill = Brushes.LightSalmon;

				Canvas.SetLeft(rectangle, g.Bounds.Left);
				Canvas.SetTop(rectangle, g.Bounds.Top);

				var res = _layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, rectangle, null);
			}
		}
	}*/

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

		public TextViewWindow(TextView view, IWpfTextViewHost viewHost)
		{
			if (view == null) throw new ArgumentNullException("view");
			if (viewHost == null) throw new ArgumentNullException("viewHost");

			_view = view;
			//AssignHandle(_view.WindowHandle);

			//HwndSource source = (HwndSource)PresentationSource.FromVisual(viewHost.HostControl);
			//source.AddHook(new HwndSourceHook(HandleMessages));
			
			var visualElement = viewHost.TextView.VisualElement;

			visualElement.KeyDown += new System.Windows.Input.KeyEventHandler(visualElement_KeyDown);
			visualElement.MouseDown += new System.Windows.Input.MouseButtonEventHandler(visualElement_MouseDown);

			//new ExperimentalAdornment(viewHost.TextView);

			//var compositionTarget = PresentationSource.FromVisual(viewHost.HostControl).CompositionTarget;
			//compositionTarget.RootVisual =
			//    new Canvas
			//    {
			//        Background = new VisualBrush
			//        {
			//            Visual = viewHost.HostControl, 
			//            ViewboxUnits = BrushMappingMode.Absolute,
			//            ViewportUnits = BrushMappingMode.Absolute
			//        }
			//    };

			Log.Debug("TextViewWindow is created");
		}

		void visualElement_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			HandleUserInput();
		}

		void visualElement_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			HandleUserInput();
		}

		public void Dispose()
		{
			//ReleaseHandle();
		}
		/*
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
		*/
		private void HandleUserInput()
		{
			string text = _view.GetSelectedText();

			if (text != _previousSelectedText)
			{
				Log.Debug("Selected text: '{0}'", text);

				_previousSelectedText = text;
				_view.SearchText(text);
			}
		}

		//private void OnPaint(Rectangle clipRect)
		//{
		//    Monitor.Enter(_paintSync);

		//    User32.HideCaret(Handle);

		//    using (Graphics g = Graphics.FromHwnd(Handle))
		//    {
		//        if (clipRect == Rectangle.Empty)
		//        {
		//            clipRect = Rectangle.Truncate(g.VisibleClipBounds);
		//        }

		//        g.SetClip(clipRect);

		//        var evt = Paint;
		//        if (evt != null) evt(this, new PaintEventArgs(g, clipRect));
		//    }

		//    User32.ShowCaret(Handle);

		//    var paintEndEvent = PaintEnd;
		//    if (paintEndEvent != null) paintEndEvent(this, EventArgs.Empty);

		//    Monitor.Exit(_paintSync);
		//}
	}
}
