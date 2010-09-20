using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
    public class SearchCompletedEventArgs : EventArgs
    {
        private TextOccurences _occurences;

        public TextOccurences Occurences
        {
            get { return _occurences; }
        }

        public SearchCompletedEventArgs(TextOccurences occurences)
        {
            _occurences = occurences;
        }
    }
}
