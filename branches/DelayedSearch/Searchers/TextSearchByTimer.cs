using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using Microsoft.VisualStudio.TextManager.Interop;
using WordLight.Extensions;

namespace WordLight.Searchers
{
    public class TextSearchByTimer : ITextSearch
    {
        private System.Timers.Timer searchTimer;
        private SearchJob _job;

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        public TextSearchByTimer()
        {
            searchTimer = new System.Timers.Timer();
            searchTimer.AutoReset = false;
            searchTimer.Interval = 500;
            searchTimer.Elapsed += new ElapsedEventHandler(searchTimer_Elapsed);
        }

        public void SearchAsync(IVsTextLines buffer, string text, int topTextLineInView, int bottomTextLineInView)
        {
            searchTimer.Stop();

            _job = new SearchJob()
            {
                Buffer = buffer,
                Text = text
            };

            if (string.IsNullOrEmpty(text))
            {
                EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
                if (evt != null) evt(this, new SearchCompletedEventArgs(_job, null));
            }
            else
            {
                TextSpan searchRange = buffer.CreateSpanForAllLines();

                searchRange.iStartLine = topTextLineInView;
                if (searchRange.iEndLine != bottomTextLineInView)
                {
                    searchRange.iEndLine = bottomTextLineInView;
                    searchRange.iEndIndex = 0;
                }

                searchTimer.Start();

                IList<SearchMark> marks = buffer.SearchWords(text, searchRange);
                
                _job.Range = searchRange;
                EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
                if (evt != null) evt(this, new SearchCompletedEventArgs(_job, marks));
            }
        }

        private void searchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_job.Text))
            {
                TextSpan searchRange = _job.Buffer.CreateSpanForAllLines();
                IList<SearchMark> marks = _job.Buffer.SearchWords(_job.Text, searchRange);

                _job.Range = searchRange;
                EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
                if (evt != null) evt(this, new SearchCompletedEventArgs(_job, marks));
            }
        }
    }
}
