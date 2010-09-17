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
					_root.ForEachInOrder((x) => { OnDeleteMark(new TextMark(x, _markLength)); });
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
					_root.ForEachInOrder((x) => { OnDeleteMark(new TextMark(x, _markLength)); });
					_root = null;
				}

				if (newMarks != null && newMarks.Length > 0)
				{
					_markLength = newMarks[0].Length;

					int[] xs = new int[newMarks.Length];
					for (int i = 0; i < newMarks.Length; i++)
					{
						xs[i] = newMarks[i].Start;
						OnAddMark(newMarks[i]);
					}

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
					_markLength = newMarks[0].Length;

					int[] xs = new int[newMarks.Length];
					for (int i = 0; i < newMarks.Length; i++)
						xs[i] = newMarks[i].Start;

					var n = Treap.Build(xs);

					if (n != null)
					{
						if (_root != null)
						{
							n.ForEachLessThan(_root.GetMinX(), (x) => { OnAddMark(new TextMark(x, _markLength)); });
							n.ForEachGreaterThan(_root.GetMaxX(), (x) => { OnAddMark(new TextMark(x, _markLength)); });
						}
						else
						{
							n.ForEachInOrder((x) => { OnAddMark(new TextMark(x, _markLength)); });
						}
					}

					_root = n;
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
				_root.Split(start - 1, out _root, out right);
				right.Split(end, out garbage, out right);

				if (garbage != null)
					garbage.ForEachInOrder((x) => { OnDeleteMark(new TextMark(x, _markLength)); });

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
					right.ForEachInOrder((x) => { xlist.Add(x + tailOffset); });
					_root = Treap.Merge(_root, Treap.Build(xlist.ToArray()));
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
					_root.ForEachInOrderBetween(
						view.VisibleTextStart - _markLength,
						view.VisibleTextEnd + _markLength,
						(x) =>
						{
							Rectangle rect = view.GetRectangleForMark(x, _markLength);
							if (rect != Rectangle.Empty)
							{
								rect.Width -= 1;
								rect.Height -= 1;

								var intersectedRect = Rectangle.Intersect(clip, rect);
								if (intersectedRect != Rectangle.Empty)								
								{
									if (rectList == null) 
										rectList = new List<Rectangle>();
									rectList.Add(intersectedRect);
								}
							}
						}
					);
				}
			}

			if (rectList != null)
				return rectList.ToArray();

			return null;
		}
	}
}
