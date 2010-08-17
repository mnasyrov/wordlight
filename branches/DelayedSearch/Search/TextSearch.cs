using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

using Microsoft.VisualStudio.TextManager.Interop;
using WordLight.Extensions;

namespace WordLight.Search
{
    public class TextSearch
    {
        private class SearchJob
        {
            public string Value { get; set; }
            public TextSpan Range { get; set; }
        }

        const int SearchDelay = 250; //ms

        private IVsTextLines _buffer;

        private System.Timers.Timer _searchTimer;
        private SearchJob _delayedJob = new SearchJob();
        private object _delayedSearchSyncLock = new object();

        private Queue<SearchJob> _asyncJobs;
        private object _asyncJobsSyncRoot = new object();
        private bool _isThreadWorking;
        private object _isThreadWorkingSyncRoot = new object();

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        public TextSearch(IVsTextLines buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");

            _buffer = buffer;

            _searchTimer = new System.Timers.Timer();
            _searchTimer.AutoReset = false;
            _searchTimer.Interval = SearchDelay;
            _searchTimer.Elapsed += new ElapsedEventHandler(searchTimer_Elapsed);

            _asyncJobs = new Queue<SearchJob>();
        }

        /// <remarks>
        /// Modification of Boyer–Moore string search
        /// Based on http://algolist.manual.ru/search/esearch/qsearch.php
        /// </remarks>
        private List<int> SearchOccurrencesInText(string text, string value, int searchStart, int searchEnd)
        {
            List<int> results = new List<int>();

            int[] badChars = new int[char.MaxValue + 1];
            int valueLength = value.Length;

            /* Preprocessing */
            for (int i = 0; i < badChars.Length; i++)
                badChars[i] = valueLength + 1;

            for (int i = 0; i < valueLength; i++)
                badChars[value[i]] = valueLength - i;

            /* Searching */
            searchEnd = Math.Min(searchEnd, text.Length) - valueLength;
            for (int i = searchStart; i < searchEnd; i += badChars[text[i + valueLength]])
            {
                if (text.Substring(i, valueLength).StartsWith(value, StringComparison.InvariantCultureIgnoreCase))
                    results.Add(i);
            }

            return results;
        }

        public IList<TextSpan> SearchOccurrences(string value, TextSpan searchRange)
        {
            List<TextSpan> marks = new List<TextSpan>();

            string text = _buffer.GetText();

            if (!string.IsNullOrEmpty(text))
            {
                int searchStart = _buffer.GetPositionOfLineIndex(searchRange.iStartLine, searchRange.iStartIndex);
                int searchEnd = _buffer.GetPositionOfLineIndex(searchRange.iEndLine, searchRange.iEndIndex);

                int length = value.Length;

                if (searchEnd > searchStart && length > 0)
                {
                    List<int> positions = SearchOccurrencesInText(text, value, searchStart, searchEnd);

                    foreach (int pos in positions)
                    {
                        TextSpan span = new TextSpan();
                        _buffer.GetLineIndexOfPosition(pos, out span.iStartLine, out span.iStartIndex);
                        _buffer.GetLineIndexOfPosition(pos + length, out span.iEndLine, out span.iEndIndex);

                        //Do not process multi-line selections
                        if (span.iStartLine == span.iEndLine)
                        {
                            marks.Add(span);
                        }
                    }
                }
            }

            return marks;
        }

        #region Delayed searching

        public void SearchOccurrencesDelayed(string value, TextSpan searchRange)
        {
            _searchTimer.Stop();

            if (!string.IsNullOrEmpty(value))
            {
                lock (_delayedSearchSyncLock)
                {
                    _delayedJob.Value= value;
                    _delayedJob.Range = searchRange;
                }
                _searchTimer.Start();
            }
        }

        private void searchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string value;
            TextSpan searchRange;

            lock (_delayedSearchSyncLock)
            {
                value = _delayedJob.Value;
                searchRange = _delayedJob.Range;
            }

            IList<TextSpan> marks = SearchOccurrences(value, searchRange);

            EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
            if (evt != null && marks.Count > 0)
            {
                evt(this, new SearchCompletedEventArgs(value, searchRange, marks));
            }
        }

        #endregion

        #region Async searching

        public void SearchOccurrencesAsync(string value, TextSpan searchRange)
        {
            if (!string.IsNullOrEmpty(value))
            {
                lock (_asyncJobsSyncRoot)
                {
                    _asyncJobs.Enqueue(new SearchJob() { Value = value, Range = searchRange });
                }

                if (!_isThreadWorking)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(SearchThreadWorker));
                }
            }
        }

        private void SearchThreadWorker(object stateInfo)
        {
            lock (_isThreadWorkingSyncRoot)
            {
                if (_isThreadWorking)
                {
                    return;
                }
                _isThreadWorking = true;
            }

            SearchJob job;
            do
            {
                // Dequeue job
                lock (_asyncJobsSyncRoot)
                {
                    job = (_asyncJobs.Count > 0 ? _asyncJobs.Dequeue() : null);
                }

                if (job != null)
                {
                    IList<TextSpan> marks = SearchOccurrences(job.Value, job.Range);

                    EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
                    if (evt != null)
                    {
                        evt(this, new SearchCompletedEventArgs(job.Value, job.Range, marks));
                    }
                }
            }
            while (job != null);

            lock (_isThreadWorkingSyncRoot)
            {
                _isThreadWorking = false;
            }
        }

        #endregion
    }
}
