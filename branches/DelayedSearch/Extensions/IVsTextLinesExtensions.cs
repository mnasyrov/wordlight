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
                List<int> positions = SearchOccurrence(content, text, searchStart, searchEnd);

                foreach (int pos in positions)
                {
                    TextSpan span = new TextSpan();
                    buffer.GetLineIndexOfPosition(pos, out span.iStartLine, out span.iStartIndex);
                    buffer.GetLineIndexOfPosition(pos + length, out span.iEndLine, out span.iEndIndex);

                    //Do not process multi-line selections
                    if (span.iStartLine == span.iEndLine)
                    {
                        marks.Add(span);
                    }
                }
            }

            return marks;
        }
        
        /// <remarks>
        /// Modification of Boyer–Moore string search
        /// Based on http://algolist.manual.ru/search/esearch/qsearch.php
        /// </remarks>
        private static List<int> SearchOccurrence(string text, string value, int searchStart, int searchEnd)
        {
            List<int> results = new List<int>();

            int[] badChars = new int[char.MaxValue + 1];
            int valueLength = value.Length;

            /* Preprocessing */
            for (int i = 0; i < badChars.Length; i++)
                badChars[i] = valueLength + 1;

            for (int i = 0; i < valueLength; i++)
                badChars[value[i]] = valueLength - i;

            /* Searching */
            searchEnd = Math.Min(searchEnd, text.Length) - valueLength;
            for (int i = searchStart; i < searchEnd; i += badChars[text[i + valueLength]])
            {
                if (text.Substring(i, valueLength).StartsWith(value, StringComparison.InvariantCultureIgnoreCase))
                    results.Add(i);
            }

            return results;
        }
    }
}
