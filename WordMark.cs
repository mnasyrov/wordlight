using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;

namespace WordLight
{
	public class WordMark
	{
		public int StartLine;
		public int StartLineIndex;
		public int EndLine;
		public int EndLineIndex;

		public void Draw(Graphics g, IVsTextView view)
		{
			int lineHeight;
			view.GetLineHeight(out lineHeight);

			POINT[] startPoint = new POINT[1];
			view.GetPointOfLineColumn(
				StartLine - 1,
				StartLineIndex - 1,
				startPoint
			);

			POINT[] endPoint = new POINT[1];
			view.GetPointOfLineColumn(
				EndLine - 1,
				EndLineIndex - 1,
				endPoint
			);

			int height = endPoint[0].y - startPoint[0].y + lineHeight;
			int width = endPoint[0].x - startPoint[0].x;

			int x = startPoint[0].x;
			int y = startPoint[0].y;

			using (Pen greenPen = new Pen(Color.Lime, 1))
			{
				g.DrawRectangle(greenPen, x, y, width, height);
			}
		}
	}
}
