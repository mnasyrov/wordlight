using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using WordLight.NativeMethods;
using WordLight.Search;

namespace WordLight
{
    public class ScreenUpdateManager
    {
        private const int MaxScreenWidth = 10000;

        private IntPtr _hWnd;
        private TextView _view;
        private object _updateRectSync = new object();

        private int _start = int.MaxValue;
        private int _end = int.MinValue;

        public ScreenUpdateManager(IntPtr hWnd, TextView view)
        {
            _hWnd = hWnd;
            _view = view;
        }

        public void IncludeMark(TextMark mark)
        {
            lock (_updateRectSync)
            {
                if (_view.IsVisible(mark))
                {
                    if (mark.Start < _start)
                        _start = mark.Start;

                    if (mark.End > _end)
                        _end = mark.End;
                }
            }
        }

        public void IncludeText(int position, int length)
        {
            lock (_updateRectSync)
            {
                if (_view.IsVisibleText(position, length))
                {
                    if (position < _start)
                        _start = position;

                    int textEnd = position + length;
                    if (textEnd > _end)
                        _end = textEnd;
                }
            }
        }


        public void CompleteUpdate()
        {
            lock (_updateRectSync)
            {
                if (_start != int.MaxValue)
                {
                    var rect = GetRect(_start, _end);
                    if (rect != Rectangle.Empty)
                        User32.ValidateRect(_hWnd, rect);

                    _start = int.MaxValue;
                    _end = int.MinValue;
                }
            }
        }

        public void RequestUpdate()
        {
            lock (_updateRectSync)
            {
                if (_start != int.MaxValue)
                {
                    var rect = GetRect(_start, _end);
                    if (rect != Rectangle.Empty)
                        User32.InvalidateRect(_hWnd, rect, false);
                }
            }
        }

        private Rectangle GetRect(int textStart, int textEnd)
        {
            textStart = Math.Max(textStart, _view.VisibleTextStart);
            textEnd = Math.Min(textEnd, _view.VisibleTextEnd);

            var rect = _view.GetRectangle(new TextMark(textStart, textEnd - textStart));
			if (rect != Rectangle.Empty)
			{
				rect.X = 0;
                rect.Width = MaxScreenWidth;
			}
            return rect;
        }
    }
}

