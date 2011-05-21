using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WordLight.EventAdapters;

namespace WordLight.Search
{
	public class MarkSearcher
	{
		private int _id;
		private string _searchText;
		private TextSearch _searcher;
		private MarkCollection _marks;

		private ITextViewAdapter _view;

		public int Id
		{
			get { return _id; }
		}

		public MarkCollection Marks
		{
			get { return _marks; }
		}

		public string SearchText
		{
			get { return _searchText; }
		}

		public MarkSearcher(int id, ITextViewAdapter view)
		{
			if (view == null) throw new ArgumentNullException("view");

			_view = view;

			_id = id;

			_searcher = new TextSearch(_view);
			_searcher.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(AsyncSearchCompleted);

			_marks = new MarkCollection(view.ScreenUpdater);
		}

		public void Clear()
		{
			_searchText = string.Empty;
			_marks.Clear();
			_searcher.ResetSearch();
		}

		public void Search(string selectedText)
		{
			Clear();

			if (!string.IsNullOrEmpty(selectedText))
			{
				FreezeText(selectedText);
			}
		}

		public void FreezeText(string selectedText)
		{
			_searchText = selectedText;

			var instantMarks = _searcher.SearchOccurrences(_searchText, _view.VisibleTextStart, _view.VisibleTextEnd);
			_marks.ReplaceMarks(instantMarks);

			_searcher.SearchOccurrencesDelayed(_searchText, 0, int.MaxValue);
		}

		public void OnTextChanged(int newPosition, int newLength, int oldPosition, int oldLength)
		{
			if (!string.IsNullOrEmpty(_searchText))
			{
				try
				{
					int searchStart = newPosition - _searchText.Length;
					int searchEnd = newPosition + newLength + _searchText.Length;

					searchStart = Math.Max(0, searchStart);

					var occurences = _searcher.SearchOccurrences(_searchText, searchStart, searchEnd);

					int replacementStart = oldPosition;
					int replacementEnd = oldPosition + oldLength;

					_marks.ReplaceMarks(occurences, replacementStart, replacementEnd, newLength - oldLength);
				}
				catch (Exception ex)
				{
					Log.Error("Failed to process text changes", ex);
				}
			}		
		}

		private void AsyncSearchCompleted(object sender, SearchCompletedEventArgs e)
		{
			try
			{
				if (e.Occurences.Text == _searchText)
				{
					_marks.AddMarks(e.Occurences);
				}
			}
            catch (Exception ex)
            {
                Log.Error("Failed to add marks after searching", ex);
            }
		}	
	}
}
