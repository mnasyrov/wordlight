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
			public int SearchStart { get; set; }
			public int SearchEnd { get; set; }
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

            /* Preprocessing */
            int valueLength = value.Length;            
			var badChars = new Dictionary<int, int>(valueLength);
            
            for (int i = 0; i < valueLength; i++)
            {
                int key = value[i];
				badChars[key] = valueLength - i;
            }

            /* Searching */
			searchEnd = Math.Min(searchEnd, text.Length) - (valueLength - 1);
            for (int i = searchStart; i < searchEnd; )
            {
                if (text.Substring(i, valueLength).StartsWith(value, StringComparison.InvariantCultureIgnoreCase))
                    results.Add(i);

				if (i + valueLength >= searchEnd)
					break;

				int key = text[i + valueLength];
				if (badChars.ContainsKey(key))
					i += badChars[key];
				else
					i += valueLength + 1;
            }

            return results;
        }

		public TextMark[] SearchOccurrences(string value, int searchStart, int searchEnd)
        {
            var marks = new List<TextMark>();

            if (!string.IsNullOrEmpty(value))
            {
                string text = _buffer.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    int length = value.Length;

                    if (searchEnd >= searchStart && length > 0)
                    {
                        List<int> positions = SearchOccurrencesInText(text, value, searchStart, searchEnd);

                        foreach (int pos in positions)
                        {
							marks.Add(new TextMark(pos, length));
                        }
                    }
                }
            }

            return marks.ToArray();
        }

        #region Delayed searching

		public void SearchOccurrencesDelayed(string value, int searchStart, int searchEnd)
        {
            _searchTimer.Stop();

            if (!string.IsNullOrEmpty(value))
            {
                lock (_delayedSearchSyncLock)
                {
                    _delayedJob.Value = value;
					_delayedJob.SearchStart = searchStart;
					_delayedJob.SearchEnd = searchEnd;
                }
                _searchTimer.Start();
            }
        }

        private void searchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            string value;
            int searchStart;
			int searchEnd;

            lock (_delayedSearchSyncLock)
            {
                value = _delayedJob.Value;
                searchStart = _delayedJob.SearchStart;
				searchEnd = _delayedJob.SearchEnd;
            }

			SearchOccurrencesAsync(value, searchStart, searchEnd);
        }

        #endregion

        #region Async searching

		public void SearchOccurrencesAsync(string value, int searchStart, int searchEnd)
        {
            lock (_asyncJobsSyncRoot)
            {
                _asyncJobs.Enqueue(new SearchJob() { Value = value, SearchStart = searchStart, SearchEnd = searchEnd });
            }

            if (!_isThreadWorking)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(SearchThreadWorker));
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
                    var marks = SearchOccurrences(job.Value, job.SearchStart, job.SearchEnd);

                    EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
                    if (evt != null)
                    {
						evt(this, new SearchCompletedEventArgs(job.Value, job.SearchStart, job.SearchEnd, marks));
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
