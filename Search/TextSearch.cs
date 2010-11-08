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

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        private bool _caseSensitiveSearch;
        private bool _searchWholeWordsOnly;

        public TextSearch(TextView view)
        {
            if (view == null) throw new ArgumentNullException("view");

			_view = view;

            _searchTimer = new System.Timers.Timer();
            _searchTimer.AutoReset = false;
            _searchTimer.Interval = SearchDelay;
            _searchTimer.Elapsed += new ElapsedEventHandler(SearchTimer_Elapsed);

            _asyncJobs = new Queue<SearchJob>();

            _caseSensitiveSearch = AddinSettings.Instance.CaseSensitiveSearch;
            _searchWholeWordsOnly = AddinSettings.Instance.SearchWholeWordsOnly;
        }

        private static bool IsWordCharacter(char c)
        {
            return 
                char.IsLetterOrDigit(c) ||
                char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.ConnectorPunctuation;
        }

        /// <remarks>
        /// Modification of Boyer–Moore string search
        /// Based on http://algolist.manual.ru/search/esearch/qsearch.php
        /// </remarks>
        private TextOccurences SearchOccurrencesInText(string text, string value, int searchStart, int searchEnd)
        {
			int textLength = text.Length;
			int valueLength = value.Length;

			//Make sure, that the search range is not out of the text
			searchStart = Math.Max(0, searchStart);
			searchEnd = Math.Min(searchEnd, textLength);

            var positions = new TreapBuilder();

            /* Preprocessing */            
            var badChars = new Dictionary<int, int>(valueLength);

            for (int i = 0; i < valueLength; i++)
            {
                char c = value[i];

                if (_caseSensitiveSearch)
                    badChars[c] = valueLength - i;
                else
                {
                    badChars[char.ToLower(c)] = valueLength - i;
                    badChars[char.ToUpper(c)] = valueLength - i;
                }
            }

            /* Searching */
            var comparsion = (_caseSensitiveSearch ? 
                StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);

            int searchLoopEnd = Math.Min(searchEnd, text.Length) - (valueLength - 1);
			for (int i = searchStart; i < searchLoopEnd; )
            {
                if (text.Substring(i, valueLength).StartsWith(value, comparsion))
                {
                    bool occurrenceFound = true;

                    if (_searchWholeWordsOnly)
                    {
                        int previousCharIndex = i - 1;
                        int nextCharIndex = i + valueLength;

                        bool isPreviousCharPartOfWord = previousCharIndex >= 0 && IsWordCharacter(text[previousCharIndex]);
                        bool isNextCharPartOfWord = nextCharIndex < textLength && IsWordCharacter(text[nextCharIndex]);

                        occurrenceFound = !isPreviousCharPartOfWord && !isNextCharPartOfWord;
                    }
                    
                    if (occurrenceFound)
                        positions.Add(i);

                    //Don't search inside a found substring (no crossed search marks).
                    i += valueLength; 
                    continue;
                }

                if (i + valueLength >= searchEnd)
                    break;

                int key = text[i + valueLength];
                if (badChars.ContainsKey(key))
                    i += badChars[key];
                else
                    i += valueLength + 1;
            }

            return new TextOccurences(value, positions);
        }

        public TextOccurences SearchOccurrences(string value, int searchStart, int searchEnd)
        {
            if (!string.IsNullOrEmpty(value))
            {
                //Disabled searching of multi line text
                if (!value.Contains('\n'))
                {
                    string text = _view.Buffer.GetText();
                    if (!string.IsNullOrEmpty(text))
                    {
                        int length = value.Length;

                        if (searchEnd >= searchStart && length > 0)
                        {
                            return SearchOccurrencesInText(text, value, searchStart, searchEnd);
                        }
                    }
                }
            }

            return TextOccurences.Empty;
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
                    var occurences = SearchOccurrences(job.Value, job.SearchStart, job.SearchEnd);

                    EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
                    if (evt != null)
                    {
                        evt(this, new SearchCompletedEventArgs(occurences));
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
