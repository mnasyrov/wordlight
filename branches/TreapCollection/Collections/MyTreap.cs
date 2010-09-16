using System.Collections;
using System.Text;
using System;

namespace WordLight.Collections
{
    // http://habrahabr.ru/blogs/algorithm/101818/
    public class MyTreap<TKey, TValue> where TKey : IComparable<TKey>
    {
        private Random _randomPriority;
        private TreapNode<TKey, TValue> _root;

        public TreapNode<TKey, TValue> Root
        {
            get { return _root; }
        }

        public MyTreap()
        {
            _randomPriority = new Random();
        }

        public void Clear()
        {
            _root = null;
        }

        public void Add(TKey key, TValue value)
        {
            TreapNode<TKey, TValue> l = null;
            TreapNode<TKey, TValue> r = null;
            
            if (_root != null)
                _root.Split(key, out l, out r);

            var newNode = new TreapNode<TKey, TValue>()
            {
                Key = key, 
                Value = value,
                Priority = _randomPriority.Next()
            };

            l = TreapNode<TKey, TValue>.Merge(l, newNode);
            _root = TreapNode<TKey, TValue>.Merge(l, r);
        }

        public TreapNode<TKey, TValue> Remove(int x)
        {
            Treap l, m, r;
            Split(x - 1, out l, out r);
            r.Split(x, out m, out r);
            return Merge(l, r);
        }

        ///<summary>
        /// Add
        /// args: ByVal key As IComparable, ByVal data As Object
        /// key is object that implements IComparable interface
        ///</summary>
        public void Add(IComparable key, object data)
        {
            if (key == null) throw new ArgumentNullException("key");

            // create New node
            TreapNode node = new TreapNode();
            node.Key = key;
            node.Data = data;

            // generate random priority
            node.Priority = _randomPriority.Next();

            // insert node into treapTree
            boolKeyFound = false;
            _root = InsertNode(node, _root);
            if (boolKeyFound)
                throw (new TreapException("A Node with the same key already exists"));
            else
                _size = _size + 1;
        }

        ///<summary>
        /// InsertNode
        /// inserts a node into the tree - note recursive method
        /// this method rebalances the tree using the priorities
        ///
        /// Note: The lower the number, the higher the priority
        ///<summary>
        private TreapNode InsertNode(TreapNode node, TreapNode tree)
        {
            if (tree == null)
                return node;

            int result = node.Key.CompareTo(tree.Key);

            if (result < 0)
            {
                tree.Left = InsertNode(node, tree.Left);
                if (tree.Left.Priority < tree.Priority)
                    tree = tree.RotateRight();
            }
            else
                if (result > 0)
                {
                    tree.Right = InsertNode(node, tree.Right);
                    if (tree.Right.Priority < tree.Priority)
                        tree = tree.RotateLeft();
                }
                else
                {
                    //prevData = tree.Data;
                    tree.Data = node.Data;
                }

            return tree;
        }

        ///<summary>
        /// GetData
        /// Gets the data associated with the specified key
        ///<summary>
        public object GetData(IComparable key)
        {
            TreapNode treeNode = _root;
            int result;

            while (treeNode != null)
            {
                result = key.CompareTo(treeNode.Key);
                if (result == 0)
                    return treeNode.Data;
                if (result < 0)
                    treeNode = treeNode.Left;
                else
                    treeNode = treeNode.Right;
            }

            throw (new TreapException("Treap key was not found"));
        }
        ///<summary>
        /// GetMinKey
        /// Returns the minimum key value
        ///<summary>
        public IComparable GetMinKey()
        {
            TreapNode treeNode = _root;

            if (treeNode == null)
                throw (new TreapException("Treap is empty"));

            while (treeNode.Left != null)
                treeNode = treeNode.Left;

            return treeNode.Key;

        }
        ///<summary>
        /// GetMaxKey
        /// Returns the maximum key value
        ///<summary>
        public IComparable GetMaxKey()
        {
            TreapNode treeNode = _root;

            if (treeNode == null)
                throw (new TreapException("Treap is empty"));

            while (treeNode.Right != null)
                treeNode = treeNode.Right;

            return treeNode.Key;

        }
        ///<summary>
        /// GetMinValue
        /// Returns the object having the minimum key value
        ///<summary>
        public object GetMinValue()
        {
            return GetData(GetMinKey());
        }
        ///<summary>
        /// GetMaxValue
        /// Returns the object having the maximum key
        ///<summary>
        public object GetMaxValue()
        {
            return GetData(GetMaxKey());
        }
        ///<summary>
        /// GetEnumerator
        ///<summary>
        public TreapEnumerator GetEnumerator()
        {
            return Elements(true);
        }
        ///<summary>
        /// Keys
        /// If ascending is True, the keys will be returned in ascending order, else
        /// the keys will be returned in descending order.
        ///<summary>
        public TreapEnumerator Keys()
        {
            return Keys(true);
        }
        public TreapEnumerator Keys(bool ascending)
        {
            return new TreapEnumerator(_root, true, ascending);
        }
        ///<summary>
        /// Values
        /// .NET compatibility
        ///<summary>
        public TreapEnumerator Values()
        {
            return Elements(true);
        }
        ///<summary>
        /// Elements
        /// Returns an enumeration of the data objects.
        /// If ascending is true, the objects will be returned in ascending order,
        /// else the objects will be returned in descending order.
        ///<summary>
        public TreapEnumerator Elements()
        {
            return Elements(true);
        }
        public TreapEnumerator Elements(bool ascending)
        {
            return new TreapEnumerator(_root, false, ascending);
        }
        ///<summary>
        /// IsEmpty
        ///<summary>
        public bool IsEmpty()
        {
            return (_root == null);
        }
        ///<summary>
        /// Remove
        /// removes the key and Object
        ///<summary>
        public void Remove(IComparable key)
        {
            if (key == null)
                throw (new TreapException("Treap key is null"));

            boolKeyFound = false;

            _root = Delete(key, _root);

            if (boolKeyFound)
                _size = _size - 1;
        }

        ///<summary>
        /// RemoveMin
        /// removes the node with the minimum key
        ///<summary>
        public object RemoveMin()
        {
            if (_root == null)
                throw (new TreapException("Treap is null"));

            // start at top
            TreapNode treeNode = _root;
            TreapNode prevTreapNode;

            if (treeNode.Left == null)
                // remove top node by replacing with right
                _root = treeNode.Right;
            else
            {
                do
                {
                    // find the minimum node
                    prevTreapNode = treeNode;
                    treeNode = treeNode.Left;
                } while (treeNode.Left != null);
                // remove left node by replacing with right node
                prevTreapNode.Left = treeNode.Right;
            }

            _size = _size - 1;

            return treeNode.Data;

        }
        ///<summary>
        /// RemoveMax
        /// removes the node with the maximum key
        ///<summary>
        public object RemoveMax()
        {
            if (_root == null)
                throw (new TreapException("Treap is null"));

            // start at top
            TreapNode treeNode = _root;
            TreapNode prevTreapNode;

            if (treeNode.Right == null)
                // remove top node by replacing with left
                _root = treeNode.Left;
            else
            {
                do
                {
                    // find the maximum node
                    prevTreapNode = treeNode;
                    treeNode = treeNode.Right;
                } while (treeNode.Right != null);
                // remove right node by replacing with left node
                prevTreapNode.Right = treeNode.Left;
            }

            _size = _size - 1;

            return treeNode.Data;
        }

        ///<summary>
        /// Delete
        /// deletes a node - note recursive function
        /// Deletes works by "bubbling down" the node until it is a leaf, and then
        /// pruning it off the tree
        ///<summary>
        private TreapNode Delete(IComparable key, TreapNode tNode)
        {
            if (tNode == null)
                return null;

            int result = key.CompareTo(tNode.Key);

            if (result < 0)
                tNode.Left = Delete(key, tNode.Left);
            else
                if (result > 0)
                    tNode.Right = Delete(key, tNode.Right);
                else
                {
                    boolKeyFound = true;
                    //prevData = tNode.Data;
                    tNode = tNode.DeleteRoot();
                }

            return tNode;
        }



        ///<summary>
        /// RotateLeft
        /// Rebalance the tree by rotating the nodes to the left
        ///</summary>
        private TreapNode<TKey, TValue> RotateLeft(TreapNode<TKey, TValue> node)
        {
            var temp = node.Right;
            node.Right = node.Right.Left;
            temp.Left = node;
            return temp;
        }

        ///<summary>
        /// RotateRight
        /// Rebalance the tree by rotating the nodes to the right
        ///</summary>
        private TreapNode<TKey, TValue> RotateRight(TreapNode<TKey, TValue> node)
        {
            var temp = node.Left;
            node.Left = node.Left.Right;
            temp.Right = node;
            return temp;
        }

        ///<summary>
        /// DeleteRoot
        /// If one of the children is an empty subtree, remove the root and put the other
        /// child in its place. If both children are nonempty, rotate the treapTree at
        /// the root so that the child with the smallest priority number comes to the
        /// top, then delete the root from the other subtee.
        ///
        /// NOTE: This method is recursive
        ///</summary>
        private TreapNode<TKey, TValue> DeleteRoot(TreapNode<TKey, TValue> node)
        {
            if (node.Left == null)
                return node.Right;

            if (node.Right == null)
                return node.Left;

            TreapNode<TKey, TValue> temp;

            if (node.Left.Priority < node.Right.Priority)
            {
                temp = RotateRight(node);
                temp.Right = DeleteRoot(node);
            }
            else
            {
                temp = RotateLeft(node);
                temp.Left = DeleteRoot(node);
            }

            return temp;
        }
    }
}
