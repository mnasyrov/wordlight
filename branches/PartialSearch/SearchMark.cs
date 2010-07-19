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

		private POINT[] _startPoint;
		private POINT[] _endPoint;

		private int _lineHeight;
        private IVsTextView _view;

		public SearchMark(IVsTextView view, int lineHeight, EditPoint start, EditPoint end)
		{
            _view = view;
			_lineHeight = lineHeight;

			_startLine = start.Line - 1;
			_startLineIndex = start.LineCharOffset - 1;
			_endLine = end.Line - 1;
			_endLineIndex = end.LineCharOffset - 1;

            _startPoint = new POINT[1];
            _endPoint = new POINT[1];
		}

		public Rectangle GetVisibleRectangle(RectangleF visibleClipBounds)
		{
			_view.GetPointOfLineColumn(_startLine, _startLineIndex, _startPoint);
			_view.GetPointOfLineColumn(_endLine, _endLineIndex, _endPoint);
			
            bool isVisible =
				visibleClipBounds.Left <= _endPoint[0].x && _startPoint[0].x <= visibleClipBounds.Right
				&& visibleClipBounds.Top <= _endPoint[0].y && _startPoint[0].y <= visibleClipBounds.Bottom;

            if (isVisible)
            {
                int height = _endPoint[0].y - _startPoint[0].y + _lineHeight;
                int width = _endPoint[0].x - _startPoint[0].x;

                int x = _startPoint[0].x;
                int y = _startPoint[0].y;

                return new Rectangle(x, y, width, height);
            }

            return Rectangle.Empty;
		}
	}
}
