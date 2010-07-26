using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight.Searchers
{
    public class SearchCompletedEventArgs : EventArgs
    {
        private SearchJob _job;
        private IList<SearchMark> _marks;

        public SearchJob Job
        {
            get { return _job; }
        }

        public IList<SearchMark> Marks
        {
            get { return _marks; }
        }

        public SearchCompletedEventArgs(SearchJob job, IList<SearchMark> marks)
        {
            _job = job;
            _marks = marks;
        }
    }
}
