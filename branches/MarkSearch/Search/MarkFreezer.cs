using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WordLight.EventAdapters;

namespace WordLight.Search
{
	public class MarkFreezer
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

		public MarkFreezer(int id, TextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

			_view = view;

			_id = id;

			searcher = new TextSearch(_view);
			searcher.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(AsyncSearchCompleted);

			_marks = new MarkCollection(view);

			view.TextStreamEvents.StreamTextChanged += 
				new EventHandler<StreamTextChangedEventArgs>(StreamTextChangedHandler);
		}

		public void Clear()
		{
			_searchText = string.Empty;
			_marks.Clear();
		}

		public void FreezeText(string selectedText)
		{
			_searchText = selectedText;

			var instantMarks = searcher.SearchOccurrences(_searchText, _view.VisibleTextStart, _view.VisibleTextEnd);
			_marks.ReplaceMarks(instantMarks);

			searcher.SearchOccurrencesDelayed(_searchText, 0, int.MaxValue);
		}

		private void StreamTextChangedHandler(object sender, StreamTextChangedEventArgs e)
		{
			if (!string.IsNullOrEmpty(_searchText))
			{
				int searchStart = e.Position - _searchText.Length;
				int searchEnd = e.Position + e.NewLength + _searchText.Length;

				searchStart = Math.Max(0, searchStart);

				var occurences = searcher.SearchOccurrences(_searchText, searchStart, searchEnd);

				int replacementStart = e.Position;
				int replacementEnd = e.Position + e.OldLength;

				_marks.ReplaceMarks(occurences, replacementStart, replacementEnd, e.NewLength - e.OldLength);
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
