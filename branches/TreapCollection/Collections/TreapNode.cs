using System;
using System.Text;

namespace WordLight.Collections
{
    ///<summary>
    /// The TreapNode class encapsulates a node in the treap
    ///</summary>
    public class TreapNode<TKey, TValue> where TKey : IComparable<TKey>
    {
        public int Priority { get; set; }
        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public TreapNode<TKey, TValue> Left { get; set; }
        public TreapNode<TKey, TValue> Right { get; set; }

        public TreapNode()
        {
        }

        public TreapNode(TKey key, TValue value, int priority, TreapNode<TKey, TValue> left, TreapNode<TKey, TValue> right)
        {
            Key = key;
            Value = value;
            Priority = priority;
            Left = left;
            Right = right;
        }

        public static TreapNode<TKey, TValue> Merge(TreapNode<TKey, TValue> L, TreapNode<TKey, TValue> R)
        {
            if (L == null) return R;
            if (R == null) return L;

            if (L.Priority > R.Priority)
            {
                var newR = Merge(L.Right, R);
                return new TreapNode<TKey, TValue>(L.Key, L.Value, L.Priority, L.Left, newR);
            }
            else
            {
                var newL = Merge(L, R.Left);
                return new TreapNode<TKey, TValue>(R.Key, R.Value, R.Priority, newL, R.Right);
            }
        }

        public void Split(TKey x, out TreapNode<TKey, TValue> L, out TreapNode<TKey, TValue> R)
        {
            TreapNode<TKey, TValue> newTree = null;
            if (Key.CompareTo(x) <= 0)
            {
                if (Right == null)
                    R = null;
                else
                    Right.Split(x, out newTree, out R);
                L = new TreapNode<TKey, TValue>(Key, Value, Priority, Left, newTree);
            }
            else
            {
                if (Left == null)
                    L = null;
                else
                    Left.Split(x, out L, out newTree);
                R = new TreapNode<TKey, TValue>(Key, Value, Priority, newTree, Right);
            }
        }
    }
}
