using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.Extensions;

namespace WordLight
{
	public class TextLineCache
	{
		private int[] visibleLines;
		private int visibleLineCount;

		public TextLineCache(IVsTextView view, IVsHiddenTextManager hiddenTextManager)
		{
			IVsTextLines buffer = view.GetBuffer();
            IVsHiddenTextSession hiddenTextSession = hiddenTextManager.GetHiddenTextSession(buffer);

            TextSpan span = buffer.CreateSpanForAllLines();
            IList<TextSpan> hiddenRegions = hiddenTextSession.GetAllHiddenRegions(span);

            int lineCount = span.iEndLine + 1;

            visibleLines = new int[lineCount];
			visibleLineCount = 0;

            for (int line = 0; line < span.iEndLine; line++)
			{
                bool isVisible = true;

                foreach (var hiddenSpan in hiddenRegions)
                {
                    if (hiddenSpan.iStartLine <= line && line <= hiddenSpan.iEndIndex)
                    {
                        isVisible = false;
                        break;
                    }
                }

				if (isVisible)
				{
					visibleLines[visibleLineCount] = line;
					visibleLineCount++;
				}
			}
		}

		public int GetLineByScreenY(IVsTextView view, int targetY)
		{
			// Binary search
			int left = 0;
			int right = visibleLineCount - 1;

			while (left < right)
			{
				int middle = left + (right - left) / 2;

				Point pos = view.GetPointOfLineColumn(visibleLines[middle], 0);
				int screenY = pos.Y;

				if (targetY <= screenY)
					right = middle;
				else
					left = middle + 1;
			}

			return visibleLines[left];
		}
	}
}
