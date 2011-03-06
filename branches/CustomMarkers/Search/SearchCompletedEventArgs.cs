﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
    public class SearchCompletedEventArgs : EventArgs
    {
        private string _text;
		private TextSpan _range;
        private IList<TextSpan> _marks;

		public string Text
		{
			get { return _text; }
		}

		public TextSpan Range
		{
			get { return _range; }
		}

        public IList<TextSpan> Marks
        {
            get { return _marks; }
        }

        public SearchCompletedEventArgs(string text, TextSpan range, IList<TextSpan> marks)
        {
			_text = text;
			_range = range;
            _marks = marks;
        }
    }
}