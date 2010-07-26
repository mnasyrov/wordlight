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
		private string text;
		private IVsTextLines buffer;

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        public TextSearchByTimer()
        {
            searchTimer = new System.Timers.Timer();
            searchTimer.AutoReset = false;
            searchTimer.Interval = 500;
            searchTimer.Elapsed += new ElapsedEventHandler(searchTimer_Elapsed);
        }

		public void SearchAsync(IVsTextLines buffer, string text, TextSpan startRange)
        {
            searchTimer.Stop();

			if (string.IsNullOrEmpty(text))
				return;

			this.buffer = buffer;
			this.text = text;

			IList<SearchMark> marks = buffer.SearchWords(text, startRange);            
            
            EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
			if (evt != null && marks.Count > 0)
			{
				evt(this, new SearchCompletedEventArgs(text, startRange, marks));
			}

			searchTimer.Start();            
        }

        private void searchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!string.IsNullOrEmpty(text))
            {
                TextSpan searchRange = buffer.CreateSpanForAllLines();
                IList<SearchMark> marks = buffer.SearchWords(text, searchRange);

                EventHandler<SearchCompletedEventArgs> evt = SearchCompleted;
				if (evt != null && marks.Count > 0)
				{
					evt(this, new SearchCompletedEventArgs(text, searchRange, marks));
				}
            }
        }
    }
}
