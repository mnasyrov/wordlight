using System.Drawing;

using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight
{
	public struct SearchMark
	{
		private int _startLine;
        private int _startLineIndex;
        private int _endLine;
        private int _endLineIndex;

        public SearchMark(EditPoint start, EditPoint end)
		{
			_startLine = start.Line - 1;
			_startLineIndex = start.LineCharOffset - 1;
			_endLine = end.Line - 1;
			_endLineIndex = end.LineCharOffset - 1;
		}

        public Rectangle GetVisibleRectangle(IVsTextView view, RectangleF visibleClipBounds, int lineHeight)
		{
			Point startPoint = view.GetPointOfLineColumn(_startLine, _startLineIndex);
			Point endPoint = view.GetPointOfLineColumn(_endLine, _endLineIndex);
			
            bool isVisible =
				visibleClipBounds.Left <= endPoint.X && startPoint.X <= visibleClipBounds.Right
				&& visibleClipBounds.Top <= endPoint.Y && startPoint.Y <= visibleClipBounds.Bottom;

            if (isVisible)
            {
                int height = endPoint.Y - startPoint.Y + lineHeight;
                int width = endPoint.X - startPoint.X;

                int x = startPoint.X;
                int y = startPoint.Y;

                return new Rectangle(x, y, width, height);
            }

            return Rectangle.Empty;
		}
	}
}
