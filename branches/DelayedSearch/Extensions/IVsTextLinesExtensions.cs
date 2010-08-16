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

        public static IList<TextSpan> SearchWords(this IVsTextLines buffer, string text, TextSpan searchRange)
        {
            var all = buffer.CreateSpanForAllLines();
            
            string content;
            buffer.GetLineText(all.iStartLine, all.iStartIndex, all.iEndLine, all.iEndIndex, out content);

            List<TextSpan> marks = new List<TextSpan>();
            
            int searchStart;
            int searchEnd;
            buffer.GetPositionOfLineIndex(searchRange.iStartLine, searchRange.iStartIndex, out searchStart);
            buffer.GetPositionOfLineIndex(searchRange.iEndLine, searchRange.iEndIndex, out searchEnd);

            int length = text.Length;

            if (searchEnd > searchStart && length > 0)
            {
                bool result;
                do
                {
                    searchStart = content.IndexOf(text, searchStart, StringComparison.InvariantCultureIgnoreCase);

                    result = searchStart >= 0;
                    if (result)
                    {
                        TextSpan span = new TextSpan();
                        buffer.GetLineIndexOfPosition(searchStart, out span.iStartLine, out span.iStartIndex);
                        buffer.GetLineIndexOfPosition(searchStart + length, out span.iEndLine, out span.iEndIndex);

                        //Do not process multi-line selections
                        if (span.iStartLine == span.iEndLine)
                        {
                            marks.Add(span);
                        }
                    }
                    searchStart++;
                } while (result && searchStart <= searchEnd);
            }

            return marks;
        }
    }
}
