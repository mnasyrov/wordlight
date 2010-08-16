using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.VisualStudio.TextManager.Interop;
using WordLight.Extensions;

namespace WordLight.Searchers
{
    public class TextSearchByThreadWorker : ITextSearch
    {
        private class SearchJob
        {
            public IVsTextLines Buffer { get; set; }
            public string Text { get; set; }
            public TextSpan Range { get; set; }

            public SearchJob(IVsTextLines buffer, string text, TextSpan range)
            {
                this.Buffer = buffer;
                this.Text = text;
                this.Range = range;
            }
        }

        private Queue<SearchJob> _jobs;
        private object _jobsSyncRoot = new object();

        private bool _isThreadWorking;
        private object _isThreadWorkingSyncRoot = new object();

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        public TextSearchByThreadWorker()
        {
            _jobs = new Queue<SearchJob>();
        }

        public void SearchAsync(IVsTextLines buffer, string text, TextSpan searchRange)
        {
            if (string.IsNullOrEmpty(text))
                return;

            SearchAsync(new SearchJob(buffer, text, searchRange));
        }

        private void EnqueueJob(SearchJob job)
        {
            lock (_jobsSyncRoot)
            {
                _jobs.Enqueue(job);
            }
        }

        private SearchJob DequeueJob()
        {
            SearchJob job = null;
            lock (_jobsSyncRoot)
            {
                if (_jobs.Count > 0)
                    job = _jobs.Dequeue();
            }
            return job;
        }

        private void SearchAsync(SearchJob job)
        {
            EnqueueJob(job);

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
                job = DequeueJob();
                if (job != null)
                {
                    IList<TextSpan> marks = job.Buffer.SearchWords(job.Text, job.Range);

                    EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
                    if (evt != null)
                    {
                        evt(this, new SearchCompletedEventArgs(job.Text, job.Range, marks));
                    }
                }
            }
            while (job != null);

            lock (_isThreadWorkingSyncRoot)
            {
                _isThreadWorking = false;
            }
        }
    }
}