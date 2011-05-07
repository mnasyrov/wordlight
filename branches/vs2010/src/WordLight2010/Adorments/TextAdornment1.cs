using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;

namespace TextAdornment1
{
	///<summary>
	///TextAdornment1 places red boxes behind all the "A"s in the editor window
	///</summary>
	public class TextAdornment1
	{
		[Export(typeof(AdornmentLayerDefinition))]
		[Name("TextAdornment1")]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
		[TextViewRole(PredefinedTextViewRoles.Document)]
		internal AdornmentLayerDefinition editorAdornmentLayer;

		IAdornmentLayer _layer;
		IWpfTextView _view;
		Brush _brush;
		Pen _pen;

		public TextAdornment1(IWpfTextView view)
		{
			_view = view;
			_layer = view.GetAdornmentLayer("TextAdornment1");

			//Listen to any event that changes the layout (text changes, scrolling, etc)
			_view.LayoutChanged += OnLayoutChanged;

			//Create the pen and brush to color the box behind the a's
			Brush brush = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0xff));
			brush.Freeze();
			Brush penBrush = new SolidColorBrush(Colors.Red);
			penBrush.Freeze();
			Pen pen = new Pen(penBrush, 0.5);
			pen.Freeze();

			_brush = brush;
			_pen = pen;
		}

		/// <summary>
		/// On layout change add the adornment to any reformatted lines
		/// </summary>
		private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			foreach (ITextViewLine line in e.NewOrReformattedLines)
			{
				this.CreateVisuals(line);
			}
		}

		/// <summary>
		/// Within the given line add the scarlet box behind the a
		/// </summary>
		private void CreateVisuals(ITextViewLine line)
		{
			//grab a reference to the lines in the current TextView 
			IWpfTextViewLineCollection textViewLines = _view.TextViewLines;
			int start = line.Start;
			int end = line.End;

			//Loop through each character, and place a box around any a 
			for (int i = start; (i < end); ++i)
			{
				if (_view.TextSnapshot[i] == 'a')
				{
					SnapshotSpan span = new SnapshotSpan(_view.TextSnapshot, Span.FromBounds(i, i + 1));
					Geometry g = textViewLines.GetMarkerGeometry(span);
					if (g != null)
					{
						GeometryDrawing drawing = new GeometryDrawing(_brush, _pen, g);
						drawing.Freeze();

						DrawingImage drawingImage = new DrawingImage(drawing);
						drawingImage.Freeze();

						Image image = new Image();
						image.Source = drawingImage;

						//Align the image with the top of the bounds of the text geometry
						Canvas.SetLeft(image, g.Bounds.Left);
						Canvas.SetTop(image, g.Bounds.Top);

						_layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
					}
				}
			}
		}
	}
}
