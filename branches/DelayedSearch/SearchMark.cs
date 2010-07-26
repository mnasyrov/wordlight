﻿using System.Drawing;

using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight
{
	public struct SearchMark
	{
		private TextSpan _span;

		public TextSpan Span { get { return _span; } }

		public SearchMark(EditPoint start, EditPoint end)
		{
			_span = new TextSpan()
			{
				iStartLine = start.Line - 1,
				iStartIndex = start.LineCharOffset - 1,
				iEndLine = end.Line - 1,
				iEndIndex = end.LineCharOffset - 1
			};
		}

		public Rectangle GetVisibleRectangle(IVsTextView view, RectangleF visibleClipBounds, int lineHeight)
		{
			Rectangle rect = Rectangle.Empty;

			Point startPoint = view.GetPointOfLineColumn(_span.iStartLine, _span.iStartIndex);
			if (startPoint == Point.Empty)
				return rect;

			Point endPoint = view.GetPointOfLineColumn(_span.iEndLine, _span.iEndIndex);
			if (endPoint == Point.Empty)
				return rect;

			bool isVisible =
				visibleClipBounds.Left <= endPoint.X && startPoint.X <= visibleClipBounds.Right
				&& visibleClipBounds.Top <= endPoint.Y && startPoint.Y <= visibleClipBounds.Bottom;

			if (isVisible)
			{
				int height = endPoint.Y - startPoint.Y + lineHeight;
				int width = endPoint.X - startPoint.X;

				int x = startPoint.X;
				int y = startPoint.Y;

				rect = new Rectangle(x, y, width, height);
			}

			return rect;
		}
	}
}
