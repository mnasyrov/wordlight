///<summary>
/// The TreapEnumerator class returns the keys or objects of the treap in
/// sorted order.
///</summary>
using System;
using System.Collections;
	
namespace TreapCS
{
	public class TreapEnumerator
	{
		// the treap uses the stack to order the nodes
		private Stack stack;
		// return the keys
		private bool keys;
		// return in ascending order (true) or descending (false)
		private bool ascending;
		
		// key
		private IComparable ordKey;
		// the data or value associated with the key
		private object objValue;
		
		///<summary>
		///Key
		///</summary>
		public IComparable Key
		{
			get
            {
				return ordKey;
			}
			
			set
			{
				ordKey = value;
			}
		}
		///<summary>
		///Data
		///</summary>
		public object Value
		{
			get
            {
				return objValue;
			}
			
			set
			{
				objValue = value;
			}
		}
		
		public TreapEnumerator() 
        {
		}
		///<summary>
		/// Determine order, walk the tree and push the nodes onto the stack
		///</summary>
		public TreapEnumerator(TreapNode tnode, bool keys, bool ascending) 
        {
			
			stack           = new Stack();
			this.keys       = keys;
			this.ascending  = ascending;
			
			// find the lowest node
			if(ascending)
			{
				while(tnode != null)
				{
					stack.Push(tnode);
					tnode = tnode.Left;
				}
			}
			else
			{
				// find the highest or greatest node
				while(tnode != null)
				{
					stack.Push(tnode);
					tnode = tnode.Right;
				}
			}
			
		}
		///<summary>
		/// HasMoreElements
		///</summary>
		public bool HasMoreElements()
		{
			return (stack.Count > 0);
		}
		///<summary>
		/// NextElement
		///</summary>
		public object NextElement()
		{
			if(stack.Count == 0)
				throw(new TreapException("Element not found"));
			
			// the top of stack will always have the next item
			// get top of stack but don't remove it as the next nodes in sequence
			// may be pushed onto the top
			// the stack will be popped after all the nodes have been returned
			TreapNode node = (TreapNode) stack.Peek();
			
            if(ascending)
            {
                // if right node is nothing, the stack top is the lowest node
                // if left node is nothing, the stack top is the highest node
                if(node.Right == null)
                {
                    // walk the tree
                    TreapNode tn = (TreapNode) stack.Pop();
                    while(HasMoreElements()&& ((TreapNode) stack.Peek()).Right == tn)
                        tn = (TreapNode) stack.Pop();
                }
                else
                {
                    // find the next items in the sequence
                    // traverse to left; find lowest and push onto stack
                    TreapNode tn = node.Right;
                    while(tn != null)
                    {
                        stack.Push(tn);
                        tn = tn.Left;
                    }
                }
            }
            else    // descending
            {
                if(node.Left == null)
                {
                    // walk the tree
                    TreapNode tn = (TreapNode) stack.Pop();
                    while(HasMoreElements() && ((TreapNode)stack.Peek()).Left == tn)
                        tn = (TreapNode) stack.Pop();
                }
                else
                {
                    // find the next items in the sequence
                    // traverse to right; find highest and push onto stack
                    TreapNode tn = node.Left;
                    while(tn != null)
                    {
                        stack.Push(tn);
                        tn = tn.Right;
                    }
                }
            }
			
			// the following is for .NET compatibility (see MoveNext())
			Key     = node.Key;
			Value   = node.Data;
			
            return keys == true ? node.Key : node.Data;
			
		}
		///<summary>
		/// MoveNext
		/// For .NET compatibility
		///</summary>
		public bool MoveNext()
		{
			if(HasMoreElements())
			{
				NextElement();
				return true;
			}
			return false;
		}
	}
}
