using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight.Search
{
    public class MarkEventArgs : EventArgs
    {
        private TextMark _mark;

        public TextMark Mark
        {
            get { return _mark; }
        }

        public MarkEventArgs(TextMark mark)
        {
            _mark = mark;
        }
    }
}
