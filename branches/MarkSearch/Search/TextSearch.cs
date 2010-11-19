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

        private TextView _view;

        private System.Timers.Timer _searchTimer;
        private SearchJob _delayedJob = new SearchJob();
        private object _delayedSearchSyncLock = new object();

        private Queue<SearchJob> _asyncJobs;
        private object _asyncJobsSyncRoot = new object();
        private bool _isThreadWorking;
        private object _isThreadWorkingSyncRoot = new object();

        private BoyerMooreStringSearch _searcher;
        private object _searcherLock = new object();

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;
        

        public TextSearch(TextView view)
        {
            if (view == null) throw new ArgumentNullException("view");

			_view = view;

            _searchTimer = new System.Timers.Timer();
            _searchTimer.AutoReset = false;
            _searchTimer.Interval = SearchDelay;
            _searchTimer.Elapsed += new ElapsedEventHandler(SearchTimer_Elapsed);

            _asyncJobs = new Queue<SearchJob>();
        }        
        
        private BoyerMooreStringSearch GetSearcher(string sample)
        {
            lock (_searcherLock)
            {
                if (_searcher == null || _searcher.Sample != sample)
                    _searcher = new BoyerMooreStringSearch(sample);
                return _searcher;
            }
        }

        private TextOccurences SearchOccurrencesInText(string text, string value, int searchStart, int searchEnd)
        {
            BoyerMooreStringSearch searcher = GetSearcher(value);
            return searcher.SearchOccurrencesInText(text, searchStart, searchEnd);
        }

        public TextOccurences SearchOccurrences(string value, int searchStart, int searchEnd)
        {
            var occurences = TextOccurences.Empty;

            //Disabled searching of multi line text
            bool isValueValid = !string.IsNullOrEmpty(value) && value.Length > 0 && !value.Contains('\n');

            if (isValueValid && searchEnd >= searchStart)
            {
                try
                {
                    string text = _view.Buffer.GetText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        occurences = SearchOccurrencesInText(text, value, searchStart, searchEnd);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to search occurences", ex);
                }
            }

            return occurences;
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

        private void SearchTimer_Elapsed(object sender, ElapsedEventArgs e)
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

            try
            {
                SearchOccurrencesAsync(value, searchStart, searchEnd);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start async searching", ex);
            }
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

            try
            {
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
                        var occurences = SearchOccurrences(job.Value, job.SearchStart, job.SearchEnd);

                        EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
                        if (evt != null)
                        {
                            evt(this, new SearchCompletedEventArgs(occurences));
                        }
                    }
                }
                while (job != null);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to process async search jobs", ex);
            }

            lock (_isThreadWorkingSyncRoot)
            {
                _isThreadWorking = false;
            }
        }

        #endregion
    }
}
