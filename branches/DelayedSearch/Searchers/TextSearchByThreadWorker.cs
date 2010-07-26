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
        private Queue<SearchJob> _jobs;
        private object _jobsSyncRoot = new object();
        
        private bool _isThreadWorking;
        private object _isThreadWorkingSyncRoot = new object();

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        public TextSearchByThreadWorker()
        {
            _jobs = new Queue<SearchJob>();            
        }

        public void SearchAsync(IVsTextLines buffer, string text, int topTextLineInView, int bottomTextLineInView)
        {
            TextSpan textRange = buffer.CreateSpanForAllLines();

            TextSpan viewRange = buffer.CreateSpanForAllLines();
            viewRange.iStartLine = topTextLineInView;
            viewRange.iEndLine = bottomTextLineInView;
            if (viewRange.iEndLine == textRange.iEndLine)
            {
                viewRange.iEndIndex = textRange.iEndIndex;
            }

            SearchAsync(buffer, text, viewRange);

            int height = bottomTextLineInView - topTextLineInView;
            if (height > 0)
            {
                int upTop;
                int downBottom;
                int step = 1;

                do
                {
                    upTop = topTextLineInView - step * height;

                    TextSpan up = new TextSpan();
                    up.iStartLine = upTop < textRange.iStartLine ? textRange.iStartLine : upTop;
                    up.iEndLine = bottomTextLineInView - step * height + 1;

                    if (up.iEndLine >= textRange.iStartLine)
                    {
                        SearchAsync(buffer, text, up);
                    }

                    downBottom = bottomTextLineInView + step * height + 1;

                    TextSpan down = new TextSpan();
                    down.iStartLine = topTextLineInView + step * height;
                    down.iEndLine = downBottom > textRange.iEndLine ? textRange.iEndLine : downBottom;
                    if (down.iEndLine == textRange.iEndLine)
                    {
                        down.iEndIndex = textRange.iEndIndex;
                    }

                    if (down.iStartLine <= textRange.iEndLine)
                    {
                        SearchAsync(buffer, text, down);
                    }

                    step++;
                }
                while (upTop >= textRange.iStartLine || downBottom <= textRange.iEndLine);
            }
        }

        private void SearchAsync(IVsTextLines buffer, string text, TextSpan range)
        {
            SearchJob job = new SearchJob()
            {
                Buffer = buffer,
                Text = text,
                Range = range
            };
            SearchAsync(job);
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
                        evt(this, new SearchCompletedEventArgs(job, marks));
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
