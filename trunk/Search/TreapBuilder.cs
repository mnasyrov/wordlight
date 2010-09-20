using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight.Search
{
    public class TreapBuilder
    {
        private static Random _priorityRandom = new Random((int)DateTime.Now.Ticks);

        private Treap _first = null;
        private Treap _last = null;
        private int _count = 0;

        public int Count
        {
            get { return _count; }
        }

        public TreapBuilder()
        {
        }

        public TreapBuilder(int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Add(values[i]);
            }
        }

        public void Add(int value)
        {
            _count += 1;

            if (_first == null)
            {
                _first = new Treap(value);
                _last = _first;
                return;
            }

            int currentPriority = _priorityRandom.Next();

            if (_last.y > currentPriority)
            {
                _last.Right = new Treap() { x = value, y = currentPriority, Parent = _last };
                _last = _last.Right;
            }
            else
            {
                Treap cur = _last;
                while (cur.Parent != null && cur.y <= currentPriority)
                    cur = cur.Parent;
                if (cur.y <= currentPriority)
                    _last = new Treap() { x = value, y = currentPriority, Left = cur, Right = null };
                else
                {
                    _last = new Treap() { x = value, y = currentPriority, Left = cur.Right, Right = null, Parent = cur };
                    cur.Right = _last;
                }
            }
        }

        public Treap ToTreap()
        {
            var root = _last;
            
            if (root != null)
            {
                while (root.Parent != null)
                    root = root.Parent;
            }

            return root;
        }
    }
}
