using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight.Search
{
	//http://habrahabr.ru/blogs/algorithm/101818/
	public class Treap
	{
		private static Random _priorityRandom = new Random((int)DateTime.Now.Ticks);

		public int x;
		public int y;

		public Treap Left;
		public Treap Right;

		public Treap Parent;

		public Treap()
		{
		}

		public Treap(int x)
		{
			this.x = x;
			this.y = _priorityRandom.Next();
		}

		private Treap(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		private Treap(int x, int y, Treap left, Treap right)
		{
			this.x = x;
			this.y = y;
			this.Left = left;
			this.Right = right;
		}

		private Treap(int x, int y, Treap left, Treap right, Treap parent)
		{
			this.x = x;
			this.y = y;
			this.Left = left;
			this.Right = right;
			this.Parent = parent;
		}

		public int GetMinX()
		{
			var n = this;
			while (n.Left != null)
				n = n.Left;
			return n.x;
		}

		public int GetMaxX()
		{
			var n = this;
			while (n.Right != null)
				n = n.Right;
			return n.x;
		}

		public static void Debug(Treap root)
		{
			if (root == null)
				System.Diagnostics.Debug.WriteLine("null");
			else
				root.ForEachInOrder((x) =>
				{
					System.Diagnostics.Trace.WriteLine(x.ToString() + " ");
				});
		}

		public delegate void NodeValueAction(int x);

		public void ForEachInOrder(NodeValueAction action)
		{
			if (Left != null)
				Left.ForEachInOrder(action);
			action(x);
			if (Right != null)
				Right.ForEachInOrder(action);
		}

		public void ForEachInOrderBounded(int minX, int maxX, NodeValueAction action)
		{
			if (Left != null && Left.x >= minX)
				Left.ForEachInOrderBounded(minX, maxX, action);
			action(x);
			if (Right != null && Right.x <= maxX)
				Right.ForEachInOrderBounded(minX, maxX, action);
		}

		public static Treap Merge(Treap L, Treap R)
		{
			if (L == null) return R;
			if (R == null) return L;

			if (L.y > R.y)
			{
				var newR = Merge(L.Right, R);
				return new Treap(L.x, L.y, L.Left, newR);
			}
			else
			{
				var newL = Merge(L, R.Left);
				return new Treap(R.x, R.y, newL, R.Right);
			}
		}

		public void Split(int x, out Treap L, out Treap R)
		{
			Treap newTree = null;
			if (this.x <= x)
			{
				if (Right == null)
					R = null;
				else
					Right.Split(x, out newTree, out R);
				L = new Treap(this.x, y, Left, newTree);
			}
			else
			{
				if (Left == null)
					L = null;
				else
					Left.Split(x, out L, out newTree);
				R = new Treap(this.x, y, newTree, Right);
			}
		}

		public Treap Add(int x)
		{
			Treap l, r;
			Split(x, out l, out r);
			Treap m = new Treap(x, _priorityRandom.Next());
			return Merge(Merge(l, m), r);
		}

		public Treap Remove(int x)
		{
			Treap l, m, r;
			Split(x - 1, out l, out r);
			r.Split(x, out m, out r);
			return Merge(l, r);
		}

		public static Treap Build(int[] xs)
		{
			var tree = new Treap(xs[0]);
			var last = tree;

			for (int i = 1; i < xs.Length; ++i)
			{
				int currentPriority = _priorityRandom.Next();

				if (last.y > currentPriority)
				{
					last.Right = new Treap() { x = xs[i], y = currentPriority, Parent = last };
					last = last.Right;
				}
				else
				{
					Treap cur = last;
					while (cur.Parent != null && cur.y <= currentPriority)
						cur = cur.Parent;
					if (cur.y <= currentPriority)
						last = new Treap(xs[i], currentPriority, cur, null);
					else
					{
						last = new Treap(xs[i], currentPriority, cur.Right, null, cur);
						cur.Right = last;
					}
				}
			}

			while (last.Parent != null)
				last = last.Parent;

			return last;
		}
	}
}
