using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.EventAdapters;

namespace WordLight.Search
{
	public class MarkSearcher
	{
		private int _id;
		private string _searchText;
		private TextSearch _searcher;
		private MarkCollection _marks;

		private TextView _view;

		private List<IVsTextStreamMarker> _markers = new List<IVsTextStreamMarker>();

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

			_searcher = new TextSearch(_view);
			_searcher.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(AsyncSearchCompleted);

			_marks = new MarkCollection(view);

			view.TextStreamEvents.StreamTextChanged +=
				new EventHandler<StreamTextChangedEventArgs>(StreamTextChangedHandler);
		}

		public void Clear()
		{
			_searchText = string.Empty;
			_marks.Clear();
			_searcher.ResetSearch();

			foreach (var marker in _markers)
			{
				marker.UnadviseClient();
				marker.Invalidate();
			}
			_markers.Clear();
		}

		private void CreateMarkers(TextOccurences occurences)
		{
			IVsTextStream textStream = _view.Buffer as IVsTextStream;
			if (textStream == null)
				return;

			occurences.Positions.ForEachInOrder((int pos) =>
			{
				var marker = new IVsTextStreamMarker[1];
				textStream.CreateStreamMarker(_view.SearchMarkerTypeId, pos, occurences.TextLength, null, marker);
				_markers.Add(marker[0]);
			});
			Log.Debug("Marker count: {0}", _markers.Count);
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
			CreateMarkers(instantMarks);

			_searcher.SearchOccurrencesDelayed(_searchText, 0, int.MaxValue);
		}

		private void StreamTextChangedHandler(object sender, StreamTextChangedEventArgs e)
		{
			if (!string.IsNullOrEmpty(_searchText))
			{
				try
				{
					int searchStart = e.Position - _searchText.Length;
					int searchEnd = e.Position + e.NewLength + _searchText.Length;

					searchStart = Math.Max(0, searchStart);

					var occurences = _searcher.SearchOccurrences(_searchText, searchStart, searchEnd);
					CreateMarkers(occurences);

					int replacementStart = e.Position;
					int replacementEnd = e.Position + e.OldLength;

					_marks.ReplaceMarks(occurences, replacementStart, replacementEnd, e.NewLength - e.OldLength);
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
					CreateMarkers(e.Occurences);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Failed to add marks after searching", ex);
			}
		}
	}
}
