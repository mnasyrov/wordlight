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

		public void SearchAsync(IVsTextLines buffer, string text, TextSpan viewRange)
        {
            TextSpan textRange = buffer.CreateSpanForAllLines();

            SearchAsync(new SearchJob(buffer, text, viewRange));

			int height = viewRange.iEndLine - viewRange.iStartLine;
            if (height > 0)
            {
                int upTop;
                int downBottom;
                int step = 1;

                do
                {
					upTop = viewRange.iStartLine - step * height;

                    TextSpan up = new TextSpan();
                    up.iStartLine = upTop < textRange.iStartLine ? textRange.iStartLine : upTop;
					up.iEndLine = viewRange.iEndLine - step * height + 1;

                    if (up.iEndLine >= textRange.iStartLine)
                    {
                        SearchAsync(new SearchJob(buffer, text, up));
                    }

					downBottom = viewRange.iEndLine + step * height + 1;

                    TextSpan down = new TextSpan();
					down.iStartLine = viewRange.iStartLine + step * height;
                    down.iEndLine = downBottom > textRange.iEndLine ? textRange.iEndLine : downBottom;
                    if (down.iEndLine == textRange.iEndLine)
                    {
                        down.iEndIndex = textRange.iEndIndex;
                    }

                    if (down.iStartLine <= textRange.iEndLine)
                    {
                        SearchAsync(new SearchJob(buffer, text, down));
                    }

                    step++;
                }
                while (upTop >= textRange.iStartLine || downBottom <= textRange.iEndLine);
            }
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
                    IList<SearchMark> marks = job.Buffer.SearchWords(job.Text, job.Range);

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
