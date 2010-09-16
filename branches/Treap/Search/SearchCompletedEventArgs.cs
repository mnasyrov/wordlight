using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
    public class SearchCompletedEventArgs : EventArgs
    {
        private string _text;
		private int _searchStart;
		private int _searchEnd;
        private TextMark[] _marks;

		public string Text
		{
			get { return _text; }
		}

		public int SearchStart
		{
			get { return _searchStart; }
		}

		public int SearchEnd
		{
			get { return _searchEnd; }
		}

        public TextMark[] Marks
        {
            get { return _marks; }
        }

        public SearchCompletedEventArgs(string text, int searchStart, int searchEnd, TextMark[] marks)
        {
			_text = text;
			_searchStart = searchStart;
			_searchEnd = searchEnd;
            _marks = marks;
        }
    }
}
