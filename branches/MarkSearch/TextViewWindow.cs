using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.NativeMethods;
using WordLight.EventAdapters;
using WordLight.Extensions;
using WordLight.Search;

namespace WordLight
{
	public class TextViewWindow : NativeWindow, IDisposable
	{
		private TextView _view;

		private string _previousSelectedText;

		private List<MarkFreezer> freezers;

		private MarkCollection _searchMarks;

		private string _selectedText;		

		private TextSearch _search;

		public event EventHandler GotFocus;
		public event EventHandler LostFocus;

		private ScreenUpdateManager _screenUpdater;

		private object _paintSync = new object();

		public TextView View
		{
			get { return _view; }
		}

		public ScreenUpdateManager ScreenUpdater
		{
			get { return _screenUpdater; }
		}

		public TextViewWindow(TextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

			_view = view;

			_screenUpdater = new ScreenUpdateManager(_view.WindowHandle, _view);

            _view.ViewEvents.ScrollChanged += new EventHandler<ViewScrollChangedEventArgs>(ScrollChangedHandler);
            _view.ViewEvents.GotFocus += new EventHandler<ViewFocusEventArgs>(GotFocusHandler);
            _view.ViewEvents.LostFocus += new EventHandler<ViewFocusEventArgs>(LostFocusHandler);

			_search = new TextSearch(_view);
			_search.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(searcher_SearchCompleted);
			_searchMarks = new MarkCollection(_view);

			freezers = new List<MarkFreezer>();
			freezers.Add(new MarkFreezer(1, _view));
			freezers.Add(new MarkFreezer(2, _view));
			freezers.Add(new MarkFreezer(3, _view));			
			
			AssignHandle(_view.WindowHandle);
		}

		public void Dispose()
		{
			_view.ViewEvents.GotFocus -= GotFocusHandler;
			_view.ViewEvents.LostFocus -= LostFocusHandler;
			_view.ViewEvents.ScrollChanged -= ScrollChangedHandler;

			ReleaseHandle();
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case WinProcMessages.WM_KEYUP:
				case WinProcMessages.WM_KEYDOWN:
				case WinProcMessages.WM_LBUTTONUP:
				case WinProcMessages.WM_RBUTTONUP:
				case WinProcMessages.WM_MBUTTONUP:
				case WinProcMessages.WM_XBUTTONUP:
				case WinProcMessages.WM_LBUTTONDOWN:
				case WinProcMessages.WM_MBUTTONDOWN:
				case WinProcMessages.WM_RBUTTONDOWN:
				case WinProcMessages.WM_XBUTTONDOWN:
				case WinProcMessages.WM_LBUTTONDBLCLK:
				case WinProcMessages.WM_MBUTTONDBLCLK:
				case WinProcMessages.WM_RBUTTONDBLCLK:
				case WinProcMessages.WM_XBUTTONDBLCLK:
					base.WndProc(ref m);
					HandleUserInput();
					break;

				case WinProcMessages.WM_PAINT:
					Rectangle clipRect = User32.GetUpdateRect(Handle, false).ToRectangle();

					base.WndProc(ref m);

					if (clipRect != Rectangle.Empty)
					{
						Paint(clipRect);
					}

					break;

				default:
					base.WndProc(ref m);
					break;
			}
		}

		private void HandleUserInput()
		{
			string text = _view.GetSelectedText();

			if (text != _previousSelectedText)
			{
				_previousSelectedText = text;
				SelectionChanged(text);
			}
		}

		private void Paint(Rectangle clipRect)
		{
			Monitor.Enter(_paintSync);

			User32.HideCaret(Handle);

			using (Graphics g = Graphics.FromHwnd(Handle))
			{
				DrawSearchMarks(g, clipRect);
			}

			User32.ShowCaret(Handle);

			_screenUpdater.CompleteUpdate();

			Monitor.Exit(_paintSync);
		}

		private void DrawSearchMarks(Graphics g, Rectangle clipRect)
		{
			if (clipRect == Rectangle.Empty)
			{
				clipRect = Rectangle.Truncate(g.VisibleClipBounds);
			}

			g.SetClip(clipRect);

			DrawRectangles(_searchMarks, AddinSettings.Instance.SearchMarkBorderColor, g);

			foreach (var freezer in freezers)
			{
				Color borderColor = Color.Lime;
				switch (freezer.Id)
				{
					case 1: borderColor = AddinSettings.Instance.FreezeMark1BorderColor; break;
					case 2: borderColor = AddinSettings.Instance.FreezeMark2BorderColor; break;
					case 3: borderColor = AddinSettings.Instance.FreezeMark3BorderColor; break;
				}
				DrawRectangles(freezer.Marks, borderColor, g);
			}
		}

		private void DrawRectangles(MarkCollection marks, Color penColor, Graphics g)
		{
			Rectangle[] rectangles = marks.GetRectanglesForVisibleMarks(_view);

			if (rectangles != null && rectangles.Length > 0)
			{
				if (AddinSettings.Instance.FilledMarks)
				{
					using (var b = new SolidBrush(Color.FromArgb(32, penColor)))
						g.FillRectangles(b, rectangles);
				}

				using (var pen = new Pen(penColor))
					g.DrawRectangles(pen, rectangles);
			}
		}

		private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
		{
			if (AddinSettings.Instance.FilledMarks && Monitor.TryEnter(_paintSync))
			{
				_searchMarks.InvalidateVisibleMarks();

				foreach (var freezer in freezers)
				{
					freezer.Marks.InvalidateVisibleMarks();
				}

				_screenUpdater.RequestUpdate();

				Monitor.Exit(_paintSync);
			}
		}

		private void SelectionChanged(string text)
		{
			_selectedText = text;

			_searchMarks.Clear();

			if (!string.IsNullOrEmpty(_selectedText))
			{
                var marks = _search.SearchOccurrences(_selectedText, _view.VisibleTextStart, _view.VisibleTextEnd);
                _searchMarks.ReplaceMarks(marks);
                _search.SearchOccurrencesDelayed(_selectedText, 0, int.MaxValue);
			}

			_screenUpdater.RequestUpdate();
		}

		private void searcher_SearchCompleted(object sender, SearchCompletedEventArgs e)
		{
			if (e.Occurences.Text == _selectedText)
			{
				_searchMarks.AddMarks(e.Occurences);
			}
		}		

		private void GotFocusHandler(object sender, ViewFocusEventArgs e)
		{
			EventHandler evt = GotFocus;
			if (evt != null) evt(this, EventArgs.Empty);
		}

		private void LostFocusHandler(object sender, ViewFocusEventArgs e)
		{
			EventHandler evt = LostFocus;
			if (evt != null) evt(this, EventArgs.Empty);
		}

		public void FreezeSearch(int group)
		{
			foreach (var freezer in freezers)
			{
				if (freezer.Id == group && freezer.SearchText != _selectedText)
				{
					freezer.FreezeText(_selectedText);
				}
				else if (freezer.Id != group && freezer.SearchText == _selectedText)
				{
					freezer.Clear();
				}
			}

			_screenUpdater.RequestUpdate();
		}
	}
}
