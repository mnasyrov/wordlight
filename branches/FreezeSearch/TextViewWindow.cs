﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.DllImport;
using WordLight.EventAdapters;
using WordLight.Extensions;
using WordLight.Search;

namespace WordLight
{
    public class TextViewWindow : NativeWindow, IDisposable
    {
        private TextView _textView;

        private IVsHiddenTextManager _hiddenTextManager;
        private string _previousSelectedText;

        private MarkCollection _searchMarks = new MarkCollection();
        private MarkCollection _freezeMarks1 = new MarkCollection();
        private MarkCollection _freezeMarks2 = new MarkCollection();
        private MarkCollection _freezeMarks3 = new MarkCollection();

        private string _selectedText;
        private TextViewEventAdapter _viewEvents;
        private TextStreamEventAdapter _textStreamEvents;

        private int leftTextColumnInView = 0;

        private TextSearch _search;
        private TextSearch _freezeSearch1;
        private TextSearch _freezeSearch2;
        private TextSearch _freezeSearch3;

        private string _freezeText1;
        private string _freezeText2;
        private string _freezeText3;

        private TextSpan _viewRange = new TextSpan();

        public event EventHandler GotFocus;
        public event EventHandler LostFocus;

        private int _leftMarginWidth = 0;

        private int _visibleTextStart;
        private int _visibleTextEnd;

        private UpdateRectangle _markUpdateRect;

        private object _paintSync = new object();

        public TextViewWindow(IVsTextView view, IVsHiddenTextManager hiddenTextManager)
        {
            if (view == null) throw new ArgumentNullException("view");
            if (hiddenTextManager == null) throw new ArgumentNullException("hiddenTextManager");

            _textView = new TextView(view);

            _hiddenTextManager = hiddenTextManager;

            _textStreamEvents = new TextStreamEventAdapter(_textView.Buffer);
            _textStreamEvents.StreamTextChanged += new EventHandler<StreamTextChangedEventArgs>(StreamTextChangedHandler);

            _search = new TextSearch(_textView.Buffer);
            _search.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(searcher_SearchCompleted);

            _freezeSearch1 = new TextSearch(_textView.Buffer);
            _freezeSearch1.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted1);

            _freezeSearch2 = new TextSearch(_textView.Buffer);
            _freezeSearch2.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted2);

            _freezeSearch3 = new TextSearch(_textView.Buffer);
            _freezeSearch3.SearchCompleted += new EventHandler<SearchCompletedEventArgs>(FreezeSearchCompleted3);

            _viewEvents = new TextViewEventAdapter(_textView.View);
            _viewEvents.ScrollChanged += new EventHandler<ViewScrollChangedEventArgs>(ScrollChangedHandler);
            _viewEvents.GotFocus += new EventHandler<ViewFocusEventArgs>(GotFocusHandler);
            _viewEvents.LostFocus += new EventHandler<ViewFocusEventArgs>(LostFocusHandler);

            _searchMarks.MarkAdded += new EventHandler<MarkEventArgs>(MarkModifiedHandler);
            _searchMarks.MarkDeleted += new EventHandler<MarkEventArgs>(MarkDeletedHandler);
            _freezeMarks1.MarkAdded += new EventHandler<MarkEventArgs>(MarkModifiedHandler);
            _freezeMarks1.MarkDeleted += new EventHandler<MarkEventArgs>(MarkDeletedHandler);
            _freezeMarks2.MarkAdded += new EventHandler<MarkEventArgs>(MarkModifiedHandler);
            _freezeMarks2.MarkDeleted += new EventHandler<MarkEventArgs>(MarkDeletedHandler);
            _freezeMarks3.MarkAdded += new EventHandler<MarkEventArgs>(MarkModifiedHandler);
            _freezeMarks3.MarkDeleted += new EventHandler<MarkEventArgs>(MarkDeletedHandler);

            IntPtr hWnd = view.GetWindowHandle();
            _markUpdateRect = new UpdateRectangle(hWnd);

            AssignHandle(hWnd);
        }

        public void Dispose()
        {
            _textStreamEvents.StreamTextChanged -= StreamTextChangedHandler;
            _textStreamEvents.Dispose();

            _viewEvents.GotFocus -= GotFocusHandler;
            _viewEvents.LostFocus -= LostFocusHandler;
            _viewEvents.ScrollChanged -= ScrollChangedHandler;

            _viewEvents.Dispose();
            ReleaseHandle();
        }

        //Rectangle clipRect = Rectangle.Empty;
        //private bool isWindowScrolled;

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

                //case WinProcMessages.WM_HSCROLL:
                //case WinProcMessages.WM_VSCROLL:
                //    //Debugger("WM_?SCROLL " + m.Result);
                //    isWindowScrolled = true;
                //    break;

                case WinProcMessages.WM_PAINT:
                    //Debugger("WM_PAINT " + m.Result);

                    Rectangle clipRect = User32.GetUpdateRect(Handle, false).ToRectangle();
                    base.WndProc(ref m);

                    if (clipRect != Rectangle.Empty)
                    {
                        Paint(clipRect);
                    }

                    break;

                //case WinProcMessages.WM_ERASEBKGND:
                //    Debugger("WM_ERASEBKGND " +m.Result);
                //    base.WndProc(ref m);
                //    break;

                default:
                    //Debugger(m.Msg.ToString());
                    base.WndProc(ref m);
                    break;
            }
        }

        //private void Debugger(string message)
        //{
        //    System.Diagnostics.Debugger.Log(0, "WordLight", message + "\n");
        //}

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

            _markUpdateRect.Validate();

            Monitor.Exit(_paintSync);
        }

        private void Paint2Buf(Rectangle clip)
        {
            Monitor.Enter(_paintSync);
            User32.HideCaret(Handle);

            BufferedGraphicsContext myContext = BufferedGraphicsManager.Current;
            using (Graphics g = Graphics.FromHwnd(Handle))
            using (BufferedGraphics buf = myContext.Allocate(g, new Rectangle(0, 0, clip.Width, clip.Height)))
            {
                buf.Graphics.Clear(Color.Transparent);

                buf.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;                

                buf.Graphics.SetClip(clip);
                DrawSearchMarks(buf.Graphics, clip);
                buf.Render(g);
            }

            //Rectangle inner = new Rectangle(Point.Empty, clip.Size);
            //using (Graphics g = Graphics.FromHwnd(Handle))
            //using (BufferedGraphics bg = BufferedGraphicsManager.Current.Allocate(g, inner))
            //{
            //    using (Bitmap bmp = new Bitmap(inner.Width, inner.Height, bg.Graphics))
            //    {
            //        using (Graphics bmpg = Graphics.FromImage(bmp))
            //        {
            //            bg.Graphics.Clear(Color.Transparent);

            //            DrawSearchMarks(bg.Graphics, clip);

            //            bg.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            //            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            //            bmpg.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

            //            bg.Render(bmpg);
            //            g.DrawImageUnscaledAndClipped(bmp, clip);
            //        }
            //    }
            //}

            User32.ShowCaret(Handle);
            _markUpdateRect.Validate();
            Monitor.Exit(_paintSync);
        }

        private void DrawSearchMarks(Graphics g, Rectangle clipRect)
        {
            //Fix for clip bounds: take into account left margin pane during horizontal scrolling.
            Point leftTop = _textView.GetPointOfLineColumn(_viewRange.iStartLine, leftTextColumnInView);
            if (!leftTop.IsEmpty)
            {
                _leftMarginWidth = leftTop.X;
            }

            if (clipRect == Rectangle.Empty)
            {
                clipRect = Rectangle.Truncate(g.VisibleClipBounds);
            }

            clipRect.X = Math.Max(clipRect.X, _leftMarginWidth);

            g.SetClip(clipRect);

            DrawRectangles(_searchMarks, AddinSettings.Instance.SearchMarkBorderColor, g, clipRect);
            DrawRectangles(_freezeMarks1, AddinSettings.Instance.FreezeMark1BorderColor, g, clipRect);
            DrawRectangles(_freezeMarks2, AddinSettings.Instance.FreezeMark2BorderColor, g, clipRect);
            DrawRectangles(_freezeMarks3, AddinSettings.Instance.FreezeMark3BorderColor, g, clipRect);
        }

        private void DrawRectangles(MarkCollection marks, Color penColor, Graphics g, Rectangle clip)
        {
            Rectangle[] rectangles = marks.GetRectanglesForVisibleMarks(_visibleTextStart, _visibleTextEnd, _textView, clip);

            if (rectangles != null && rectangles.Length > 0)
            {
                using (var b = new SolidBrush(Color.FromArgb(16, penColor)))
                    g.FillRectangles(b, rectangles);

                using (var pen = new Pen(penColor))
                    g.DrawRectangles(pen, rectangles);
            }
        }

        private void MarkModifiedHandler(object sender, MarkEventArgs e)
        {
            InvalidateMark(e.Mark);
        }

        private void MarkDeletedHandler(object sender, MarkEventArgs e)
        {
            InvalidateMark(e.Mark);
        }

        private void InvalidateMark(TextMark mark)
        {
            if (mark.IsVisible(_visibleTextStart, _visibleTextEnd))
            {
                Rectangle rect = mark.GetRectangle(_textView);
                _markUpdateRect.IncludeRectangle(rect);
            }
        }

        private void InvalidateVisibleMarks(MarkCollection marks)
        {
            var clip = new Rectangle(0, 0, int.MaxValue, int.MaxValue);

            var rectangles = marks.GetRectanglesForVisibleMarks(_visibleTextStart, _visibleTextEnd, _textView, clip);

            if (rectangles != null && rectangles.Length > 0)
            {
                foreach (Rectangle rect in rectangles)
                {
                    _markUpdateRect.IncludeRectangle(rect);
                }
            }
        }

        private void ScrollChangedHandler(object sender, ViewScrollChangedEventArgs e)
        {
            _textView.ResetPointCache();

            if (e.ScrollInfo.IsHorizontal)
            {
                leftTextColumnInView = e.ScrollInfo.firstVisibleUnit;
            }

            if (e.ScrollInfo.IsVertical)
            {
                int topTextLineInView = 0;
                int bottomTextLineInView = 0;

                IVsLayeredTextView viewLayer = _textView.View as IVsLayeredTextView;
                IVsTextLayer topLayer = null;
                IVsTextLayer bufferLayer = _textView.Buffer as IVsTextLayer;

                if (viewLayer != null)
                {
                    viewLayer.GetTopmostLayer(out topLayer);
                }

                if (topLayer != null && bufferLayer != null)
                {
                    int temp;
                    topLayer.LocalLineIndexToDeeperLayer(bufferLayer, e.ScrollInfo.firstVisibleUnit, 0, out topTextLineInView, out temp);
                    topLayer.LocalLineIndexToDeeperLayer(bufferLayer, e.ScrollInfo.firstVisibleUnit + e.ScrollInfo.visibleUnits, 0, out bottomTextLineInView, out temp);
                    bottomTextLineInView++;
                }
                else
                {
                    TextSpan entireSpan = _textView.Buffer.CreateSpanForAllLines();
                    topTextLineInView = entireSpan.iStartLine;
                    bottomTextLineInView = entireSpan.iEndLine;
                }

                TextSpan viewRange = _textView.Buffer.CreateSpanForAllLines();
                viewRange.iStartLine = topTextLineInView;
                if (bottomTextLineInView < viewRange.iEndLine)
                {
                    viewRange.iEndLine = bottomTextLineInView;
                    viewRange.iEndIndex = 0;
                }

                _viewRange = viewRange;

                _visibleTextStart = _textView.Buffer.GetPositionOfLineIndex(_viewRange.iStartLine, _viewRange.iStartIndex);
                _visibleTextEnd = _textView.Buffer.GetPositionOfLineIndex(_viewRange.iEndLine, _viewRange.iEndIndex);
            }

            if (Monitor.TryEnter(_paintSync))
            {
                InvalidateVisibleMarks(_searchMarks);
                InvalidateVisibleMarks(_freezeMarks1);
                InvalidateVisibleMarks(_freezeMarks2);
                InvalidateVisibleMarks(_freezeMarks3);
                _markUpdateRect.Invalidate();
                Monitor.Exit(_paintSync);
            }
        }

        private void StreamTextChangedHandler(object sender, StreamTextChangedEventArgs e)
        {
            SearchInChangedText(_freezeSearch1, _freezeMarks1, e, _freezeText1);
            SearchInChangedText(_freezeSearch2, _freezeMarks2, e, _freezeText2);
            SearchInChangedText(_freezeSearch3, _freezeMarks3, e, _freezeText3);

            _markUpdateRect.Invalidate();
        }

        private void SearchInChangedText(TextSearch searcher, MarkCollection marks, StreamTextChangedEventArgs e, string searchText)
        {
            if (!string.IsNullOrEmpty(searchText))
            {
                int searchStart = e.Position - searchText.Length;
                int searchEnd = e.Position + e.NewLength + searchText.Length;

                var newMarks = searcher.SearchOccurrences(searchText, searchStart, searchEnd);

                int replacementStart = e.Position;
                int replacementEnd = e.Position + e.OldLength;

                marks.ReplaceMarks(newMarks, replacementStart, replacementEnd, e.NewLength - e.OldLength);
            }
        }

        private void RefreshFreezeGroups()
        {
            _freezeMarks1.ReplaceMarks(_freezeSearch1.SearchOccurrences(_freezeText1, _visibleTextStart, _visibleTextEnd));
            _freezeMarks2.ReplaceMarks(_freezeSearch2.SearchOccurrences(_freezeText2, _visibleTextStart, _visibleTextEnd));
            _freezeMarks3.ReplaceMarks(_freezeSearch3.SearchOccurrences(_freezeText3, _visibleTextStart, _visibleTextEnd));

            _freezeSearch1.SearchOccurrencesDelayed(_freezeText1, 0, int.MaxValue);
            _freezeSearch2.SearchOccurrencesDelayed(_freezeText2, 0, int.MaxValue);
            _freezeSearch3.SearchOccurrencesDelayed(_freezeText3, 0, int.MaxValue);

            _markUpdateRect.Invalidate();
        }

        private void SelectionChanged(string text)
        {
            _selectedText = text;

            _searchMarks.Clear();

            if (!string.IsNullOrEmpty(_selectedText))
            {
                var marks = _search.SearchOccurrences(_selectedText, _visibleTextStart, _visibleTextEnd);
                _searchMarks.ReplaceMarks(marks);
                _search.SearchOccurrencesDelayed(_selectedText, 0, int.MaxValue);
            }

            _markUpdateRect.Invalidate();
        }

        private void searcher_SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            if (e.Text == _selectedText)
            {
                _searchMarks.ReplaceMarks(e.Marks);
                _markUpdateRect.Invalidate();
            }
        }

        private void FreezeSearchCompleted1(object sender, SearchCompletedEventArgs e)
        {
            _freezeMarks1.ReplaceMarks(e.Marks);
            _markUpdateRect.Invalidate();
        }

        private void FreezeSearchCompleted2(object sender, SearchCompletedEventArgs e)
        {
            _freezeMarks2.ReplaceMarks(e.Marks);
            _markUpdateRect.Invalidate();
        }

        private void FreezeSearchCompleted3(object sender, SearchCompletedEventArgs e)
        {
            _freezeMarks3.ReplaceMarks(e.Marks);
            _markUpdateRect.Invalidate();
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
            switch (group)
            {
                case 1:
                    _freezeText1 = _selectedText;
                    break;
                case 2:
                    _freezeText2 = _selectedText;
                    break;
                case 3:
                    _freezeText3 = _selectedText;
                    break;
            }

            RefreshFreezeGroups();
        }
    }
}
