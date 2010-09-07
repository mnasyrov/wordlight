using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
	public class MarkCollection
	{
		private LinkedList<TextMark> _marks = new LinkedList<TextMark>();
		private object _marksSyncRoot = new object();

		public void Clear()
		{
			lock (_marksSyncRoot)
			{
				_marks.Clear();
			}
		}

		public void ReplaceMarks(TextMark[] newMarks)
		{
			lock (_marksSyncRoot)
			{
				if (newMarks == null || newMarks.Length == 0)
					_marks.Clear();
				else
					_marks = new LinkedList<TextMark>(newMarks);
			}
		}

		public void ReplaceMarks(TextMark[] newMarks, int start, int end, int tailOffset)
		{
			lock (_marksSyncRoot)
			{
				if (_marks.Count == 0)
				{
					ReplaceMarks(newMarks);
					return;
				}

				var left = _marks.First;
				var right = _marks.Last;

				for (var node = _marks.First; node != null; node = node.Next)
				{
					if (node.Value.End >= start)
						break;
					left = node;
				}

				for (var node = _marks.Last; node != null && node != left; node = node.Previous)
				{
					node.Value.Start += tailOffset;
					node.Value.End += tailOffset;

					if (node.Value.Start <= end)
						break;
					right = node;
				}

				for (var node = left.Next; node != null && node != right; node = node.Next)
				{
					_marks.Remove(node);
				}

				if (newMarks != null)
				{
					for (int i = 0; i < newMarks.Length; i++)
					{
						left = _marks.AddAfter(left, newMarks[i]);
					}
				}
			}
		}

		public Rectangle[] GetRectanglesForVisibleMarks(int viewStart, int viewEnd, Rectangle visibleClipBounds, IVsTextView view, int lineHeight, IVsTextBuffer buffer)
		{
			List<Rectangle> rectList = null;

			lock (_marksSyncRoot)
			{
				for (var node = _marks.First; node != null; node = node.Next)
				{
					if (node.Value.End < viewStart || node.Value.Start > viewEnd)
						continue;

					TextSpan mark = new TextSpan();
					buffer.GetLineIndexOfPosition(node.Value.Start, out mark.iStartLine, out mark.iStartIndex);
					buffer.GetLineIndexOfPosition(node.Value.End, out mark.iEndLine, out mark.iEndIndex);

					//Do not process multi-line selections
					if (mark.iStartLine != mark.iEndLine)
					{
						continue;
					}

					//GetVisibleRectangle
					Point startPoint = view.GetPointOfLineColumn(mark.iStartLine, mark.iStartIndex);
					if (startPoint == Point.Empty)
						continue;

					Point endPoint = view.GetPointOfLineColumn(mark.iEndLine, mark.iEndIndex);
					if (endPoint == Point.Empty)
						continue;

					bool isVisible =
						visibleClipBounds.Left <= endPoint.X && startPoint.X <= visibleClipBounds.Right
						&& visibleClipBounds.Top <= endPoint.Y && startPoint.Y <= visibleClipBounds.Bottom;

					if (isVisible)
					{
						int x = Math.Max(startPoint.X, visibleClipBounds.Left);
						int y = startPoint.Y;

						int height = endPoint.Y - y + lineHeight;
						int width = endPoint.X - x;

						if (rectList == null)
							rectList = new List<Rectangle>();

						rectList.Add(new Rectangle(x, y, width, height));
					}
				}
			}

			if (rectList != null)
				return rectList.ToArray();

			return null;
		}
	}
}
