using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight.Search
{
    public class TextOccurences
    {
        private static TextOccurences _empty = new TextOccurences();

        private string _text;
        private int _textLength;
        private Treap _positions;
        private int _count;

        public string Text
        {
            get { return _text; }
        }

        public int TextLength
        {
            get { return _textLength; }
        }

        public int Count
        {
            get { return _count; }
        }

        public Treap Positions
        {
            get { return _positions; }
        }

        public static TextOccurences Empty
        {
            get { return _empty; }
        }

        private TextOccurences()
        {
            _text = string.Empty;
            _textLength = 0;
            _positions = null;
            _count = 0;
        }

        public TextOccurences(string text, TreapBuilder positions)
        {
            if (text == null) throw new ArgumentNullException("text");
            if (positions == null) throw new ArgumentNullException("positions");

            _text = text;
            _textLength = text.Length;
            _positions = positions.ToTreap();
            _count = positions.Count;
        }
    }
}
