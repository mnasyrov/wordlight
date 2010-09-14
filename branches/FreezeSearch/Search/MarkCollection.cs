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
        public event EventHandler<MarkEventArgs> MarkDeleted;
        public event EventHandler<MarkEventArgs> MarkAdded;

		private LinkedList<TextMark> _marks = new LinkedList<TextMark>();
		private object _marksSyncRoot = new object();

        private HashSet<int> _markSet = new HashSet<int>();

        private void OnDeleteMark(TextMark mark)
        {
            EventHandler<MarkEventArgs> evt = MarkDeleted;
            if (evt != null)
            {
                evt(this, new MarkEventArgs(mark));
            }
        }

        private void OnAddMark(TextMark mark)
        {
            EventHandler<MarkEventArgs> evt = MarkAdded;
            if (evt != null)
            {
                evt(this, new MarkEventArgs(mark));
            }
        }

		public void Clear()
		{
			lock (_marksSyncRoot)
			{
                foreach (var mark in _marks)
                {
                    OnDeleteMark(mark);
                }

				_marks.Clear();
                _markSet.Clear();
			}
		}

        public void ReplaceMarks(TextMark[] newMarks)
        {
            lock (_marksSyncRoot)
            {
                foreach (var mark in _marks)
                {
                    OnDeleteMark(mark);
                }

                _marks.Clear();
                _markSet.Clear();

                foreach (var mark in newMarks)
                {
                    if (_markSet.Add(mark.Start))
                    {
                        _marks.AddLast(mark);
                        OnAddMark(mark);
                    }
                }
            }
        }

		public void AddMarks(TextMark[] newMarks)
		{
			lock (_marksSyncRoot)
			{
                foreach (var mark in newMarks)
                {
                    if (_markSet.Add(mark.Start))
                    {
                        _marks.AddLast(mark);
                        OnAddMark(mark);
                    }
                }
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

                //Determine left and right bounds
				var left = _marks.First;
				var right = _marks.Last;

				for (var node = _marks.First; node != null; node = node.Next)
				{
					if (node.Value.End > start)
						break;
					left = node;
				}                

				for (var node = _marks.Last; node != null && node != left; node = node.Previous)
				{
					node.Value.Start += tailOffset;

					if (node.Value.Start < end)
						break;
					right = node;
				}

                //Delete deprecated marks
                for (var node = left.Next; node != null && node != right; node = node.Next)
                {
                    OnDeleteMark(node.Value);
                    _marks.Remove(node);
                }
                
                //rebuild index
                _markSet.Clear();
                foreach (var mark in _marks)
                    _markSet.Add(mark.Start);

                //Add new marks instead of old ones
				if (newMarks != null)
				{
					for (int i = 0; i < newMarks.Length; i++)
					{
                        if (_markSet.Add(newMarks[i].Start))
                        {
						    left = _marks.AddAfter(left, newMarks[i]);
                            OnAddMark(newMarks[i]);
                        }
					}
				}
			}
		}

		public Rectangle[] GetRectanglesForVisibleMarks(int visibleTextStart, int visibleTextEnd, TextView view, Rectangle clip)
		{
			List<Rectangle> rectList = null;

			lock (_marksSyncRoot)
			{
				for (var node = _marks.First; node != null; node = node.Next)
				{
					TextMark mark = node.Value;

                    if (view.IsVisible(mark, visibleTextStart, visibleTextEnd))
					{
                        Rectangle rect = view.GetRectangle(mark);
						if (rect != Rectangle.Empty)
						{
							if (rectList == null)
								rectList = new List<Rectangle>();

							rect.Width -= 1;
							rect.Height -= 1;

                            Rectangle r = Rectangle.Intersect(clip, rect);
                            if (r != Rectangle.Empty)
                                rectList.Add(rect);
						}
					}
				}
			}

			if (rectList != null)
				return rectList.ToArray();

			return null;
		}
	}
}
