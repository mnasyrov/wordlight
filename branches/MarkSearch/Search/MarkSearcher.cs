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
		private TextSearch searcher;
		private MarkCollection _marks;

		private TextView _view;

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

		public MarkSearcher(int id, TextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

			_view = view;

			_id = id;

			searcher = new TextSearch(_view);
			searcher.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(AsyncSearchCompleted);

			_marks = new MarkCollection(view);
		}

		public void Clear()
		{
			_searchText = string.Empty;
			_marks.Clear();
		}

		public void Search(string selectedText)
		{
			Clear();

			if (!string.IsNullOrEmpty(selectedText))
			{
				var instantMarks = searcher.SearchOccurrences(selectedText, _view.VisibleTextStart, _view.VisibleTextEnd);
				_marks.ReplaceMarks(instantMarks);
				searcher.SearchOccurrencesDelayed(selectedText, 0, int.MaxValue);
			}
		}
		
		private void AsyncSearchCompleted(object sender, SearchCompletedEventArgs e)
		{
			if (e.Occurences.Text == _searchText)
			{
				_marks.AddMarks(e.Occurences);
			}
		}
	}
}
