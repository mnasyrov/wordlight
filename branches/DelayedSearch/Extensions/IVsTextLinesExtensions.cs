using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight
{
    public static class IVsTextLinesExtensions
    {
        public static TextSpan CreateSpanForAllLines(this IVsTextBuffer buffer)
        {
            TextSpan span = new TextSpan();
            buffer.GetLastLineIndex(out span.iEndLine, out span.iEndIndex);
            return span;
        }

        public static EditPoint CreateEditPoint(this IVsTextLines buffer, int line, int column)
        {
            object tempPointer;
            buffer.CreateEditPoint(line, column, out tempPointer);
            return tempPointer as EditPoint;
        }

        public static IList<SearchMark> SearchWords(this IVsTextLines buffer, string text, TextSpan searchRange)
        {
            List<SearchMark> marks = new List<SearchMark>();

            EditPoint searchStart = buffer.CreateEditPoint(searchRange.iStartLine, searchRange.iStartIndex);
            EditPoint searchEnd = buffer.CreateEditPoint(searchRange.iEndLine, searchRange.iEndIndex);

            if (searchStart != null && searchEnd != null)
            {
                bool result;
                TextRanges ranges = null;
                EditPoint wordEnd = null;
                
                do
                {
                    result = searchStart.FindPattern(text, (int)vsFindOptions.vsFindOptionsNone, ref wordEnd, ref ranges);
                    if (result)
                    {
                        //Do not process multi-line selections
						if (searchStart.Line == wordEnd.Line)
						{
							marks.Add(new SearchMark(searchStart, wordEnd));
						}
                    }
                    searchStart = wordEnd;
                } while (result && searchStart.LessThan(searchEnd));
            }

            return marks;
        }
    }
}
