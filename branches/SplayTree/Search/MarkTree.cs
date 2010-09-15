using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight.Search
{
    public class MarkTree : SplayTree<TextMark>
    {
        public TextMark FindMarkAfterPosition(int position)
        {
            var found = FindMarkAfterPosition(position, Root);

            if (found != null)
                Splay(found);

            return found;
        }

        private TextMark FindMarkAfterPosition(int position, SplayTreeNode<TextMark> node)
        {
            if (node == null)
                return null;

            TextMark found = null;

            if (node.Key.Start == position)
                found = node.Key;
            else if (node.Key.Start >= position)
                found = FindMarkAfterPosition(position, node.Right);
            else
            {            
                found = FindMarkAfterPosition(position, node.Left);
                if (found == null)
                    found = node.Key;
            }

            return found;
        }
    }
}
