using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

using WordLight.EventAdapters;
using WordLight.Extensions;
using WordLight.Search;
using WordLight;

using WordLight2010;

namespace WordLight2010
{
	public class MarkAdornment : ITextViewAdapter
	{
		IAdornmentLayer _layer;
		IWpfTextView _view;
		Brush _brush;
		Pen _pen;

		string _previousSelectedText;

		//private IVsTextLines _buffer;
		//private TextViewEventAdapter _viewEvents;

		private object _windowLock = new object();

		private ScreenUpdateManager _screenUpdater;

		private double _lineHeight = 0;

		private Dictionary<long, Point> _pointCache = new Dictionary<long, Point>();
		private object _pointCacheSync = new object();

		//private TextSpan _visibleSpan = new TextSpan();
		private int _visibleTextStart;
		private int _visibleTextEnd;
		private int _visibleLeftTextColumn = 0;

		private MarkSearcher selectionSearcher;
		private MarkSearcher freezer1;
		private MarkSearcher freezer2;
		private MarkSearcher freezer3;
		private List<MarkSearcher> freezers;

		public MarkAdornment(IWpfTextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

			_view = view;
			_layer = view.GetAdornmentLayer("MarkAdornment");
			
			_view.Selection.SelectionChanged += new System.EventHandler(Selection_SelectionChanged);
			_view.Closed += new System.EventHandler(_view_Closed);

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

			/////////////////////////
			//_buffer = view.GetBuffer();

			//_viewEvents = new TextViewEventAdapter(view);
			//_viewEvents.ScrollChanged += ScrollChangedHandler;

			_screenUpdater = new ScreenUpdateManager();

			//_lineHeight = _view.LineHeight;

			selectionSearcher = new MarkSearcher(-1, this);
			freezer1 = new MarkSearcher(1, this);
			freezer2 = new MarkSearcher(2, this);
			freezer3 = new MarkSearcher(3, this);

			freezers = new List<MarkSearcher>();
			freezers.Add(freezer1);
			freezers.Add(freezer2);
			freezers.Add(freezer3);

			_view.TextBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(TextBuffer_Changed);
		}

		#region Properties

		//public IVsTextLines Buffer
		//{
		//    get { return _buffer; }
		//}

		public int LineHeight
		{
			get { return (int)_lineHeight; }
		}

		//public TextSpan VisibleSpan
		//{
		//    get { return _visibleSpan; }
		//}

		public int VisibleTextStart
		{
			get { return _visibleTextStart; }
		}

		public int VisibleTextEnd
		{
			get { return _visibleTextEnd; }
		}

		public int VisibleLeftTextColumn
		{
			get { return _visibleLeftTextColumn; }
		}

		public IScreenUpdateManager ScreenUpdater
		{
			get { return _screenUpdater; }
		}

		#endregion


		void _view_Closed(object sender, System.EventArgs e)
		{
			//_viewEvents.ScrollChanged -= ScrollChangedHandler;
			//_viewEvents.Dispose();
		}

		void Selection_SelectionChanged(object sender, System.EventArgs e)
		{
			var selection = (ITextSelection)sender;

			string text = string.Empty;

			if (selection.Mode == TextSelectionMode.Stream)
			{
				text = selection.StreamSelectionSpan.GetText();
			}

			if (text != _previousSelectedText)
			{
				Log.Debug("Selected text: '{0}'", text);

				_previousSelectedText = text;
				SearchText(text);
			}
		}

		/// <summary>
		/// On layout change add the adornment to any reformatted lines
		/// </summary>
		private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			foreach (ITextViewLine line in e.NewOrReformattedLines)
			{
				CreateVisualMarks(line, selectionSearcher.Marks, AddinSettings.Instance.SearchMarkBorderColor);
				CreateVisualMarks(line, freezer1.Marks, AddinSettings.Instance.FreezeMark1BorderColor);
				CreateVisualMarks(line, freezer2.Marks, AddinSettings.Instance.FreezeMark2BorderColor);
				CreateVisualMarks(line, freezer3.Marks, AddinSettings.Instance.FreezeMark3BorderColor);
			}
		}

		/// <summary>
		/// Within the given line add the scarlet box behind the a
		/// </summary>
		private void CreateVisualMarks(ITextViewLine line, MarkCollection marks, System.Drawing.Color markColor)
		{
			//grab a reference to the lines in the current TextView 
			IWpfTextViewLineCollection textViewLines = _view.TextViewLines;
			int start = line.Start;
			int end = line.End;

			//Rectangle[] rectangles = marks.GetRectanglesForVisibleMarks(this);

			//if (rectangles == null || rectangles.Length == 0)
			//    return;

			ICollection<int> positions = marks.GetMarksBetween(start, end);

			foreach (int pos in positions)
			{
				SnapshotSpan span = new SnapshotSpan(
					_view.TextSnapshot, Span.FromBounds(pos, pos + marks.MarkLength));

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

			//if (AddinSettings.Instance.FilledMarks)
			//{
			//    List<Rectangle> rectsToFilling = new List<Rectangle>();

			//    uint nativeBorderColor = (uint)markColor.R | (uint)markColor.G << 8 | (uint)markColor.B << 16;

			//    IntPtr hdc = g.GetHdc();

			//    for (int i = 0; i < rectangles.Length; i++)
			//    {
			//        var rect = rectangles[i];

			//        int score = 0;
			//        if (Gdi32.GetPixel(hdc, rect.Left, rect.Top) == nativeBorderColor) score++;
			//        if (Gdi32.GetPixel(hdc, rect.Left, rect.Bottom) == nativeBorderColor) score++;
			//        if (score < 2 && Gdi32.GetPixel(hdc, rect.Right, rect.Bottom) == nativeBorderColor) score++;
			//        if (score < 2 && Gdi32.GetPixel(hdc, rect.Right, rect.Top) == nativeBorderColor) score++;

			//        bool isBorderDrawn = score >= 2;

			//        if (!isBorderDrawn)
			//            rectsToFilling.Add(rect);
			//    }

			//    g.ReleaseHdc();

			//    if (rectsToFilling.Count > 0)
			//    {
			//        using (var bodyBrush = new SolidBrush(Color.FromArgb(32, markColor)))
			//            g.FillRectangles(bodyBrush, rectsToFilling.ToArray());

			//        //using (var borderPen = new Pen(markColor))
			//        //    g.DrawRectangles(borderPen, rectsToFilling.ToArray());
			//    }
			//}

			//Draw borders
			//using (var borderPen = new Pen(markColor))
			//    g.DrawRectangles(borderPen, rectangles);
		}


		//////////////////////////////////////////
		//////////////////////////////////////////
		//////////////////////////////////////////
		//////////////////////////////////////////

		

		

		#region IBufferTextProvider

		public string GetBufferText()
		{
			return _view.TextBuffer.CurrentSnapshot.GetText();			
		}

		void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
		{
			foreach (var change in e.Changes)
			{
				foreach (MarkSearcher searcher in freezers)
				{
					searcher.OnTextChanged(
						change.NewPosition, change.NewLength, change.OldPosition, change.OldLength);
				}
			}
		}
		
		#endregion

		public void SearchText(string text)
		{
			selectionSearcher.Search(text);
			_screenUpdater.RequestUpdate();
			
			_view.VisualElement.UpdateLayout();
		}

		public System.Drawing.Point GetScreenPoint(int line, int column)
		{
			//long pointKey = ((_visibleSpan.iStartLine & 0xFFFFL) << 32) | ((line & 0xFFFFL) << 16) | (column & 0xFFFFL);
			var screenPoint = System.Drawing.Point.Empty;

			//lock (_pointCacheSync)
			//{
			//    if (_pointCache.ContainsKey(pointKey))
			//    {
			//        screenPoint = _pointCache[pointKey];
			//    }
			//    else
			//    {
			//        var p = new Microsoft.VisualStudio.OLE.Interop.POINT[1];
			//        _view.GetPointOfLineColumn(line, column, p);

			//        screenPoint.X = p[0].x;
			//        screenPoint.Y = p[0].y;

			//        _pointCache.Add(pointKey, screenPoint);
			//    }
			//}

			return screenPoint;
		}

		//public Point GetScreenPointForTextPosition(int position)
		//{
		//    int line;
		//    int column;
		//    Buffer.GetLineIndexOfPosition(position, out line, out column);
		//    return GetScreenPoint(line, column);
		//}

		public System.Drawing.Rectangle GetRectangleForMark(int markStart, int markLength)
		{
			//Point startPoint = GetScreenPointForTextPosition(markStart);
			//if (startPoint != Point.Empty)
			//{
			//    Point endPoint = GetScreenPointForTextPosition(markStart + markLength);
			//    if (endPoint != Point.Empty)
			//    {
			//        int x = startPoint.X;
			//        int y = startPoint.Y;
			//        int height = endPoint.Y - y + LineHeight;
			//        int width = endPoint.X - startPoint.X;

			//        return new Rectangle(x, y, width, height);
			//    }
			//}
			return System.Drawing.Rectangle.Empty;
		}

		//public Rectangle GetRectangle(TextSpan span)
		//{
		//    Point startPoint = GetScreenPoint(span.iStartLine, span.iStartIndex);
		//    if (startPoint == Point.Empty)
		//        return Rectangle.Empty;

		//    Point endPoint = GetScreenPoint(span.iEndLine, span.iEndIndex);
		//    if (endPoint == Point.Empty)
		//        return Rectangle.Empty;

		//    int x = startPoint.X;
		//    int y = startPoint.Y;
		//    int height = endPoint.Y - y + LineHeight;
		//    int width = endPoint.X - x;

		//    return new Rectangle(x, y, width, height);
		//}

		public bool IsVisibleText(int position, int length)
		{
			return VisibleTextStart <= (position + length) && position <= VisibleTextEnd;
		}

		//public string GetSelectedText()
		//{
		//    return _view.GetSelectedText();
		//}

		public void FreezeSearch(int group)
		{
			foreach (var freezer in freezers)
			{
				if (freezer.Id == group && freezer.SearchText != selectionSearcher.SearchText)
				{
					freezer.FreezeText(selectionSearcher.SearchText);
				}
				else if (freezer.Id != group && freezer.SearchText == selectionSearcher.SearchText)
				{
					freezer.Clear();
				}
			}

			_screenUpdater.RequestUpdate();
		}

		//private void ResetCaches()
		//{
		//    lock (_pointCacheSync)
		//    {
		//        _pointCache.Clear();
		//    }
		//}

		//private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
		//{
		//    try
		//    {
		//        if (e.ScrollInfo.IsHorizontal)
		//        {
		//            _visibleLeftTextColumn = e.ScrollInfo.firstVisibleUnit;
		//        }

		//        if (e.ScrollInfo.IsVertical)
		//        {
		//            int topTextLineInView = 0;
		//            int bottomTextLineInView = 0;

		//            IVsLayeredTextView viewLayer = _view as IVsLayeredTextView;
		//            IVsTextLayer topLayer = null;
		//            IVsTextLayer bufferLayer = Buffer as IVsTextLayer;

		//            if (viewLayer != null)
		//            {
		//                viewLayer.GetTopmostLayer(out topLayer);
		//            }

		//            if (topLayer != null && bufferLayer != null)
		//            {
		//                int lastVisibleUnit = Math.Min(e.ScrollInfo.firstVisibleUnit + e.ScrollInfo.visibleUnits, e.ScrollInfo.maxUnit);
		//                int temp;
		//                topLayer.LocalLineIndexToDeeperLayer(bufferLayer, e.ScrollInfo.firstVisibleUnit, 0, out topTextLineInView, out temp);
		//                topLayer.LocalLineIndexToDeeperLayer(bufferLayer, lastVisibleUnit, 0, out bottomTextLineInView, out temp);
		//                bottomTextLineInView++;
		//            }
		//            else
		//            {
		//                TextSpan entireSpan = Buffer.CreateSpanForAllLines();
		//                topTextLineInView = entireSpan.iStartLine;
		//                bottomTextLineInView = entireSpan.iEndLine;
		//            }

		//            TextSpan viewRange = Buffer.CreateSpanForAllLines();
		//            viewRange.iStartLine = topTextLineInView;
		//            if (bottomTextLineInView < viewRange.iEndLine)
		//            {
		//                viewRange.iEndLine = bottomTextLineInView;
		//                viewRange.iEndIndex = 0;
		//            }

		//            _visibleSpan = viewRange;

		//            _visibleTextStart = Buffer.GetPositionOfLineIndex(_visibleSpan.iStartLine, _visibleSpan.iStartIndex);
		//            _visibleTextEnd = Buffer.GetPositionOfLineIndex(_visibleSpan.iEndLine, _visibleSpan.iEndIndex);
		//        }

		//        if (e.ScrollInfo.IsHorizontal || e.ScrollInfo.IsVertical)
		//        {
		//            ResetCaches();
		//        }
		//    }
		//    catch (Exception ex)
		//    {
		//        Log.Error("Error in scrollbar handler", ex);
		//    }
		//}

		

	}
}
