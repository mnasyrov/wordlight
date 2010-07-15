using System.Drawing;

using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight
{
	public class WordMark
	{
		public int _startLine;
		public int _startLineIndex;
		public int _endLine;
		public int _endLineIndex;

		private POINT[] _startPoint = new POINT[1];
		private POINT[] _endPoint = new POINT[1];

		private int _lineHeight;

		public WordMark(int lineHeight, EditPoint start, EditPoint end)
		{
			_lineHeight = lineHeight;

			_startLine = start.Line - 1;
			_startLineIndex = start.LineCharOffset - 1;
			_endLine = end.Line - 1;
			_endLineIndex = end.LineCharOffset - 1;
		}

		public void Draw(Graphics g, IVsTextView view, Pen pen)
		{
			view.GetPointOfLineColumn(_startLine, _startLineIndex, _startPoint);
			view.GetPointOfLineColumn(_endLine, _endLineIndex, _endPoint);

			bool isVisible =
				g.VisibleClipBounds.Left <= _endPoint[0].x && _startPoint[0].x <= g.VisibleClipBounds.Right
				&& g.VisibleClipBounds.Top <= _endPoint[0].y && _startPoint[0].y <= g.VisibleClipBounds.Bottom;

			if (isVisible)
			{
				int height = _endPoint[0].y - _startPoint[0].y + _lineHeight;
				int width = _endPoint[0].x - _startPoint[0].x;

				int x = _startPoint[0].x;
				int y = _startPoint[0].y;
                
				g.DrawRectangle(pen, x, y, width, height);
			}
		}
	}
}
