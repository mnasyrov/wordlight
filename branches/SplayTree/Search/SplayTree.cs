using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight.Search
{
    public class SplayTreeNode<T>
    {
        public T Key;          // The data in the node
        public SplayTreeNode<T> Left;         // Left child
        public SplayTreeNode<T> Right;        // Right child

        public SplayTreeNode(T key)
        {
            Key = key;
            Left = null;
            Right = null;
        }
    }

    /// <summary>
    /// A top-down splay tree.
    /// </summary>
    /// <remarks>
    /// It's based on a java-version by Danny Sleator <sleator@cs.cmu.edu>, that's available at http://www.link.cs.cmu.edu/splay/ .
    /// More details can be found at http://en.wikipedia.org/wiki/Splay_tree .
    /// </remarks>
    public class SplayTree<T> where T:IComparable<T>
    {
        private SplayTreeNode<T> _root;

        public SplayTreeNode<T> Root
        {
            get { return _root; }
        }

        public bool IsEmpty
        {
            get { return _root == null; }
        }

        public SplayTree()
        {
            _root = null;
        }

        /**
         * 
         * @param x the item to insert.
         * @throws DuplicateItemException if x is already present.
         */        
        public void Add(T key)
        {
            if (_root == null)
            {
                _root = new SplayTreeNode<T>(key);
                return;
            }
            
            Splay(key);

            int c = key.CompareTo(_root.Key);
            if (c == 0)
            {
                //	    throw new DuplicateItemException(x.toString());	    
                return;
            }

            SplayTreeNode<T> n = new SplayTreeNode<T>(key);
            if (c < 0)
            {
                n.Left = _root.Left;
                n.Right = _root;
                _root.Left = null;
            }
            else
            {
                n.Right = _root.Right;
                n.Left = _root;
                _root.Right = null;
            }
            _root = n;
        }

        /**
         * Remove from the tree.
         * @param x the item to remove.
         * @throws ItemNotFoundException if x is not found.
         */
        public void Remove(T key)
        {            
            Splay(key);

            if (key.CompareTo(_root.Key) != 0)
            {
                //            throw new ItemNotFoundException(x.toString());
                return;
            }

            // Now delete the root
            if (_root.Left == null)
            {
                _root = _root.Right;
            }
            else
            {
                SplayTreeNode<T> x = _root.Right;
                _root = _root.Left;
                Splay(key);
                _root.Right = x;
            }
        }

        /**
         * Find the smallest item in the tree.
         */
        public T FindMin()
        {
            SplayTreeNode<T> x = _root;
            
            if (_root == null)
                return default(T);

            while (x.Left != null) x = x.Left;
            Splay(x.Key);
            return x.Key;
        }

        /**
         * Find the largest item in the tree.
         */
        public T FindMax()
        {
            SplayTreeNode<T> x = _root;
            
            if (_root == null) 
                return default(T);

            while (x.Right != null) x = x.Right;
            Splay(x.Key);
            return x.Key;
        }

        /**
         * Find an item in the tree.
         */
        public T Find(T key)
        {
            if (_root == null) 
                return default(T);
            
            Splay(key);

            if (_root.Key.CompareTo(key) != 0) return default(T);
            return _root.Key;
        }        

        /** this method just illustrates the top-down method of
         * implementing the move-to-root operation 
         */
        private void MoveToRoot(T key)
        {
            SplayTreeNode<T> l, r;
            l = r = header;
            var t = _root;
            header.Left = header.Right = null;
            for (; ; )
            {
                if (key.CompareTo(t.Key) < 0)
                {
                    if (t.Left == null) break;
                    r.Left = t;                                 /* link right */
                    r = t;
                    t = t.Left;
                }
                else if (key.CompareTo(t.Key) > 0)
                {
                    if (t.Right == null) break;
                    l.Right = t;                                /* link left */
                    l = t;
                    t = t.Right;
                }
                else
                {
                    break;
                }
            }
            l.Right = t.Left;                                   /* assemble */
            r.Left = t.Right;
            t.Left = header.Right;
            t.Right = header.Left;
            _root = t;
        }

        private static SplayTreeNode<T> header = new SplayTreeNode<T>(default(T)); // For splay

        /**
         * Internal method to perform a top-down splay.
         * 
         *   splay(key) does the splay operation on the given key.
         *   If key is in the tree, then the BinaryNode containing
         *   that key becomes the root.  If key is not in the tree,
         *   then after the splay, key.root is either the greatest key
         *   < key in the tree, or the lest key > key in the tree.
         *
         *   This means, among other things, that if you splay with
         *   a key that's larger than any in the tree, the rightmost
         *   node of the tree becomes the root.  This property is used
         *   in the delete() method.
         */

        protected void Splay(T key)
        {
            SplayTreeNode<T> l, r, y;
            l = r = header;
            var t = _root;
            header.Left = header.Right = null;
            for (; ; )
            {
                if (key.CompareTo(t.Key) < 0)
                {
                    if (t.Left == null) break;
                    if (key.CompareTo(t.Left.Key) < 0)
                    {
                        y = t.Left;                            /* rotate right */
                        t.Left = y.Right;
                        y.Right = t;
                        t = y;
                        if (t.Left == null) break;
                    }
                    r.Left = t;                                 /* link right */
                    r = t;
                    t = t.Left;
                }
                else if (key.CompareTo(t.Key) > 0)
                {
                    if (t.Right == null) break;
                    if (key.CompareTo(t.Right.Key) > 0)
                    {
                        y = t.Right;                            /* rotate left */
                        t.Right = y.Left;
                        y.Left = t;
                        t = y;
                        if (t.Right == null) break;
                    }
                    l.Right = t;                                /* link left */
                    l = t;
                    t = t.Right;
                }
                else
                {
                    break;
                }
            }
            l.Right = t.Left;                                   /* assemble */
            r.Left = t.Right;
            t.Left = header.Right;
            t.Right = header.Left;
            _root = t;
        }
    }
}
