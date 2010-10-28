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
		private TextView _textView;

		private string _previousSelectedText;

		private MarkCollection _searchMarks;
		private MarkCollection _freezeMarks1;
		private MarkCollection _freezeMarks2;
		private MarkCollection _freezeMarks3;

		private string _selectedText;
		private TextStreamEventAdapter _textStreamEvents;

		private TextSearch _search;
		private TextSearch _freezeSearch1;
		private TextSearch _freezeSearch2;
		private TextSearch _freezeSearch3;

		private string _freezeText1;
		private string _freezeText2;
		private string _freezeText3;

		public event EventHandler GotFocus;
		public event EventHandler LostFocus;

		private ScreenUpdateManager _screenUpdater;

		private object _paintSync = new object();

		public TextViewWindow(IVsTextView view)
		{
			if (view == null) throw new ArgumentNullException("view");

			_textView = new TextView(view);

            IntPtr hWnd = view.GetWindowHandle();
            _screenUpdater = new ScreenUpdateManager(hWnd, _textView);

			_textStreamEvents = new TextStreamEventAdapter(_textView.Buffer);
			_textStreamEvents.StreamTextChanged += new EventHandler<StreamTextChangedEventArgs>(StreamTextChangedHandler);

            _textView.ViewEvents.ScrollChanged += new EventHandler<ViewScrollChangedEventArgs>(ScrollChangedHandler);
            _textView.ViewEvents.GotFocus += new EventHandler<ViewFocusEventArgs>(GotFocusHandler);
            _textView.ViewEvents.LostFocus += new EventHandler<ViewFocusEventArgs>(LostFocusHandler);

			_search = new TextSearch(_textView.Buffer);
			_search.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(searcher_SearchCompleted);

			_freezeSearch1 = new TextSearch(_textView.Buffer);
			_freezeSearch1.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted1);

			_freezeSearch2 = new TextSearch(_textView.Buffer);
			_freezeSearch2.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted2);

			_freezeSearch3 = new TextSearch(_textView.Buffer);
			_freezeSearch3.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted3);

            _searchMarks = new MarkCollection(_screenUpdater);
            _freezeMarks1 = new MarkCollection(_screenUpdater);
            _freezeMarks2 = new MarkCollection(_screenUpdater);
            _freezeMarks3 = new MarkCollection(_screenUpdater);
			
			AssignHandle(hWnd);
		}

		public void Dispose()
		{
			_textStreamEvents.StreamTextChanged -= StreamTextChangedHandler;
			_textStreamEvents.Dispose();

			_textView.ViewEvents.GotFocus -= GotFocusHandler;
			_textView.ViewEvents.LostFocus -= LostFocusHandler;
			_textView.ViewEvents.ScrollChanged -= ScrollChangedHandler;

			_textView.Dispose();

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
			string text = _textView.View.GetSelectedText();

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
			DrawRectangles(_freezeMarks1, AddinSettings.Instance.FreezeMark1BorderColor, g);
			DrawRectangles(_freezeMarks2, AddinSettings.Instance.FreezeMark2BorderColor, g);
			DrawRectangles(_freezeMarks3, AddinSettings.Instance.FreezeMark3BorderColor, g);
		}

		private void DrawRectangles(MarkCollection marks, Color penColor, Graphics g)
		{
			Rectangle[] rectangles = marks.GetRectanglesForVisibleMarks(_textView);

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
				_searchMarks.InvalidateVisibleMarks(_textView);
				_freezeMarks1.InvalidateVisibleMarks(_textView);
				_freezeMarks2.InvalidateVisibleMarks(_textView);
				_freezeMarks3.InvalidateVisibleMarks(_textView);

				_screenUpdater.RequestUpdate();

				Monitor.Exit(_paintSync);
			}
		}

		private void StreamTextChangedHandler(object sender, StreamTextChangedEventArgs e)
		{
			SearchInChangedText(_freezeSearch1, _freezeMarks1, e, _freezeText1);
			SearchInChangedText(_freezeSearch2, _freezeMarks2, e, _freezeText2);
			SearchInChangedText(_freezeSearch3, _freezeMarks3, e, _freezeText3);

			_screenUpdater.RequestUpdate();
		}

		private void SearchInChangedText(TextSearch searcher, MarkCollection marks, StreamTextChangedEventArgs e, string searchText)
		{
			if (!string.IsNullOrEmpty(searchText))
			{
				int searchStart = e.Position - searchText.Length;
				int searchEnd = e.Position + e.NewLength + searchText.Length;

				searchStart = Math.Max(0, searchStart);

				var occurences = searcher.SearchOccurrences(searchText, searchStart, searchEnd);

				int replacementStart = e.Position;
				int replacementEnd = e.Position + e.OldLength;

				marks.ReplaceMarks(occurences, replacementStart, replacementEnd, e.NewLength - e.OldLength);
			}
		}

		private void SelectionChanged(string text)
		{
			_selectedText = text;

			_searchMarks.Clear();

			if (!string.IsNullOrEmpty(_selectedText))
			{
                var marks = _search.SearchOccurrences(_selectedText, _textView.VisibleTextStart, _textView.VisibleTextEnd);
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
				//_markUpdateRect.Invalidate();
			}
		}

		private void FreezeSearchCompleted1(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks1.AddMarks(e.Occurences);
			//_markUpdateRect.Invalidate();
		}

		private void FreezeSearchCompleted2(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks2.AddMarks(e.Occurences);
			//_markUpdateRect.Invalidate();
		}

		private void FreezeSearchCompleted3(object sender, SearchCompletedEventArgs e)
		{
			_freezeMarks3.AddMarks(e.Occurences);
			//_markUpdateRect.Invalidate();
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
			bool set1 = group == 1 && _freezeText1 != _selectedText;
			bool set2 = group == 2 && _freezeText2 != _selectedText;
			bool set3 = group == 3 && _freezeText3 != _selectedText;

			bool erase1 = (group == 2 || group == 3) && _freezeText1 == _selectedText;
			bool erase2 = (group == 1 || group == 3) && _freezeText2 == _selectedText;
			bool erase3 = (group == 1 || group == 2) && _freezeText3 == _selectedText;					
				
			if (set1)
			{
				_freezeText1 = _selectedText;
				_freezeMarks1.ReplaceMarks(_freezeSearch1.SearchOccurrences(_freezeText1, _textView.VisibleTextStart, _textView.VisibleTextEnd));
				_freezeSearch1.SearchOccurrencesDelayed(_freezeText1, 0, int.MaxValue);
			}
			else if (erase1)
			{
				_freezeText1 = string.Empty;
				_freezeMarks1.Clear();
			}

			if (set2)
			{
				_freezeText2 = _selectedText;
				_freezeMarks2.ReplaceMarks(_freezeSearch2.SearchOccurrences(_freezeText2, _textView.VisibleTextStart, _textView.VisibleTextEnd));
				_freezeSearch2.SearchOccurrencesDelayed(_freezeText2, 0, int.MaxValue);
			}
			else if (erase2)
			{
				_freezeText2 = string.Empty;
				_freezeMarks2.Clear();
			}

			if (set3)
			{
				_freezeText3 = _selectedText;
				_freezeMarks3.ReplaceMarks(_freezeSearch3.SearchOccurrences(_freezeText3, _textView.VisibleTextStart, _textView.VisibleTextEnd));
				_freezeSearch3.SearchOccurrencesDelayed(_freezeText3, 0, int.MaxValue);
			}
			else if (erase3)
			{
				_freezeText3 = string.Empty;
				_freezeMarks3.Clear();
			}


			_screenUpdater.RequestUpdate();
		}
	}
}
