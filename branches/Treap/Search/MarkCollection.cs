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

        private int _markLength;
        private Treap _root;

        public void Clear()
        {
            lock (_marksSyncRoot)
            {
                if (_root != null)
                {
                    int start = _root.GetMinX();
                    int end = _root.GetMinX() + _markLength;
                    OnDeleteMark(new TextMark(start, end - start));
                    _root = null;
                    _markLength = 0;
                }
            }
        }

        public void ReplaceMarks(TextMark[] newMarks)
        {
            lock (_marksSyncRoot)
            {
                if (_root != null)
                {
                    int start = _root.GetMinX();
                    int end = _root.GetMinX() + _markLength;
                    OnDeleteMark(new TextMark(start, end - start));
                    _root = null;
                }

                if (newMarks != null && newMarks.Length > 0)
                {
                    int[] xs = new int[newMarks.Length];
                    for (int i = 0; i < newMarks.Length; i++)
                    {
                        xs[i] = newMarks[i].Start;
                        OnAddMark(newMarks[i]);
                    }
                    _markLength = newMarks[0].Length;

                    _root = Treap.Build(xs);
                }
            }
        }

        public void AddMarks(TextMark[] newMarks)
        {
            lock (_marksSyncRoot)
            {
                if (newMarks != null && newMarks.Length > 0)
                {
                    int[] xs = new int[newMarks.Length];
                    for (int i = 0; i < newMarks.Length; i++)
                    {
                        xs[i] = newMarks[i].Start;
                        OnAddMark(newMarks[i]);
                    }
                    _markLength = newMarks[0].Length;

                    _root = Treap.Merge(_root, Treap.Build(xs));
                }
            }
        }

        public void ReplaceMarks(TextMark[] newMarks, int start, int end, int tailOffset)
        {
            lock (_marksSyncRoot)
            {
                if (_root == null)
                {
                    ReplaceMarks(newMarks);
                    return;
                }

                Treap right, garbage;
                _root.Split(start, out _root, out right);
                right.Split(end, out garbage, out right);

                OnDeleteMark(new TextMark(start, end));


                if (newMarks != null && newMarks.Length > 0)
                {
                    int[] xs = new int[newMarks.Length];
                    for (int i = 0; i < newMarks.Length; i++)
                    {
                        xs[i] = newMarks[i].Start;
                        OnAddMark(newMarks[i]);
                    }
                    _markLength = newMarks[0].Length;

                    _root = Treap.Merge(_root, Treap.Build(xs));
                }

                if (right != null)
                {
                    List<int> xlist = new List<int>();
                    right.PushXsTo(xlist);

                    int[] xs = new int[xlist.Count];
                    for (int i = 0; i < xlist.Count; i++)
                    {
                        xs[i] = xlist[i] += tailOffset;
                    }

                    _root = Treap.Merge(_root, Treap.Build(xs));
                }
            }
        }

        public Rectangle[] GetRectanglesForVisibleMarks(TextView view, Rectangle clip)
        {
            List<Rectangle> rectList = null;

            lock (_marksSyncRoot)
            {
                if (_root != null)
                {
                    Treap left = null;
                    Treap middle = null;
                    Treap right = null;

                    _root.Split(view.VisibleTextStart - _markLength, out left, out middle);
                    if (middle != null)
                    {
                        middle.Split(view.VisibleTextEnd + _markLength, out middle, out right);
                    }

                    if (middle != null)
                    {
                        List<int> xlist = new List<int>();
                        middle.PushXsTo(xlist);

                        foreach (int x in xlist)
                        {
                            TextMark mark = new TextMark(x, _markLength);

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

                    _root = Treap.Merge(left, middle);
                    _root = Treap.Merge(_root, right);
                }
            }

            if (rectList != null)
                return rectList.ToArray();

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
