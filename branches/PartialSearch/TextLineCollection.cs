using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight
{
	public class TextLineCollection
	{
		private struct TextLine
		{
			public int Index;
			public int ScreenY;
		}

		private TextLine[] visibleLines;
		private int visibleLineCount;

		public TextLineCollection(IVsTextView view)
		{
			IVsTextLines buffer;
			view.GetBuffer(out buffer);

			int lastLine;
			int lastLineCol;
			buffer.GetLastLineIndex(out lastLine, out lastLineCol);

			visibleLines = new TextLine[lastLine + 1];
			visibleLineCount = 0;

			for (int i = 0; i < lastLine; i++)
			{
				Point pos = GetScreenPositionOfText(view, i, 0);
				if (pos != Point.Empty)
				{
					visibleLines[visibleLineCount] = new TextLine() {Index = i, ScreenY = pos.Y };
					visibleLineCount++;
				}
			}
		}

		private Point GetScreenPositionOfText(IVsTextView view, int line, int column)
		{
			var p = new Microsoft.VisualStudio.OLE.Interop.POINT[1];
			view.GetPointOfLineColumn(line, column, p);
			return new Point(p[0].x, p[0].y);
		}

		public int GetLineIndexByScreenY(int targetY, IVsTextView view)
		{
			// Binary search
			int left = 0;
			int right = visibleLineCount - 1;

			while (left < right)
			{
				int middle = left + (right - left) / 2;

				//int screenY = visibleLines[middle].ScreenY;
				Point pos = GetScreenPositionOfText(view, visibleLines[middle].Index, 0);
				int screenY = pos.Y;

				if (targetY <= screenY)
					right = middle;
				else
					left = middle + 1;
			}

			return visibleLines[left].Index;
		}
	}
}
