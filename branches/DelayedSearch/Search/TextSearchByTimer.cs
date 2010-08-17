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
        private System.Timers.Timer _searchTimer;

        private IVsTextLines _buffer;
        private string _searchText;
        private TextSpan _searchRange;
        private object _searchSyncLock = new object();

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        public TextSearchByTimer()
        {
            _searchTimer = new System.Timers.Timer();
            _searchTimer.AutoReset = false;
            _searchTimer.Interval = 500;
            _searchTimer.Elapsed += new ElapsedEventHandler(searchTimer_Elapsed);
        }

        public void SearchAsync(IVsTextLines buffer, string text, TextSpan searchRange)
        {
            _searchTimer.Stop();

            if (string.IsNullOrEmpty(text))
                return;

            lock (_searchSyncLock)
            {
                _buffer = buffer;
                _searchText = text;
                _searchRange = searchRange;
            }

            _searchTimer.Start();
        }

        private void searchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            IVsTextLines buffer;
            string searchText;
            TextSpan searchRange;

            lock (_searchSyncLock)
            {
                buffer = _buffer;
                searchText = _searchText;
                searchRange = _searchRange;
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                IList<TextSpan> marks = buffer.SearchWords(searchText, searchRange);

                EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
                if (evt != null && marks.Count > 0)
                {
                    evt(this, new SearchCompletedEventArgs(searchText, searchRange, marks));
                }
            }
        }
    }
}
