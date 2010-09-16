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

        private MarkTree _marks = new MarkTree();
		private object _marksSyncRoot = new object();

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
                while (!_marks.IsEmpty)
                {
                    OnDeleteMark(_marks.Root.Key);
                    _marks.Remove(_marks.Root.Key);
                }
			}
		}

        public void AddMarks(TextMark[] newMarks)
        {
            lock (_marksSyncRoot)
            {
                foreach (var mark in newMarks)
                {
                    if (_marks.Add(mark))
                        OnAddMark(mark);
                }
            }
        }

		public void ReplaceMarks(TextMark[] newMarks)
		{
			lock (_marksSyncRoot)
			{
                while (!_marks.IsEmpty)
                {
                    OnDeleteMark(_marks.Root.Key);
                    _marks.Remove(_marks.Root.Key);
                }

                foreach (var mark in newMarks)
                {                    
                    _marks.Add(mark);
                    OnAddMark(mark);
                }
            }
        }

        public void SilentReplaceMarks(TextMark[] newMarks)
        {
            lock (_marksSyncRoot)
            {
                //while (!_marks.IsEmpty)
                //{
                //    _marks.Remove(_marks.Root.Key);
                //}
                _marks = new MarkTree();

                foreach (var mark in newMarks)
                {
                    _marks.Add(mark);
                    OnAddMark(mark);
                }                
			}
		}

        //public void ReplaceMarks(TextMark[] newMarks, int start, int end, int tailOffset)
        //{
        //    lock (_marksSyncRoot)
        //    {
        //        if (!_marks.IsEmpty)
        //        {
        //            int pos = start;
        //            while(pos <= end)
        //            {
        //                var found = _marks.FindMarkAfterPosition(pos);
        //                if (found == null)
        //                    break;

        //                pos = found.End + 1;

        //                OnDeleteMark(found);
        //                _marks.Remove(found);
        //            }

        //            var toShift = new List<TextMark>();

        //            var shiftMark = _marks.FindMarkAfterPosition(end + 1);
        //            while (shiftMark != null)
        //            {
        //                toShift.Add(shiftMark);
        //                shiftMark = _marks.FindMarkAfterPosition(shiftMark.End + 1);
        //            }

        //            foreach (var mark in toShift)
        //            {
        //                _marks.Remove(mark);

        //                mark.Start += tailOffset;
        //                mark.IsShifted = true;

        //                _marks.Add(mark);
        //            }

        //            foreach (var mark in toShift)
        //                mark.IsShifted = false;
        //        }

        //        AddMarks(newMarks);
        //    }
        //}

        //public void RemoveMarks(int start, int end)
        //{
        //    lock (_marksSyncRoot)
        //    {
        //        if (!_marks.IsEmpty)
        //        {
        //            int pos = start;
        //            while (pos <= end)
        //            {
        //                var found = _marks.FindMarkAfterPosition(pos);
        //                if (found == null)
        //                    break;

        //                pos = found.End + 1;

        //                OnDeleteMark(found);
        //                _marks.Remove(found);
        //            }
        //        }
        //    }
        //}

		public Rectangle[] GetRectanglesForVisibleMarks(TextView view, Rectangle clip)
		{
			List<Rectangle> rectangles = null;

			lock (_marksSyncRoot)
			{
                for (int pos = view.VisibleTextStart; pos <= view.VisibleTextEnd; pos++ )
                {
                    var temp = new TextMark(pos, 0);
                    TextMark mark = _marks.Find(temp);

                    if (mark != null)
                    {
                        Rectangle rect = view.GetRectangle(mark);
                        if (rect != Rectangle.Empty)
                        {
                            if (rectangles == null)
                                rectangles = new List<Rectangle>();

                            rect.Width -= 1;
                            rect.Height -= 1;

                            Rectangle r = Rectangle.Intersect(clip, rect);
                            if (r != Rectangle.Empty)
                                rectangles.Add(rect);
                        }
                    }
                }
			}

			if (rectangles != null)
				return rectangles.ToArray();

			return null;
		}

        //public IList<TextMark> GetVisibleMarks(TextView view)
        //{
        //    List<TextMark> result = new List<TextMark>();

        //    lock (_marksSyncRoot)
        //    {
        //        for (var node = _marks.First; node != null; node = node.Next)
        //        {
        //            TextMark mark = node.Value;
        //            if (view.IsVisible(mark))
        //                result.Add(mark);
        //        }
        //    }

        //    return result;
        //}
	}
}
