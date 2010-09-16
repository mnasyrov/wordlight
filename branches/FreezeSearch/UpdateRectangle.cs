using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using WordLight.DllImport;
using WordLight.Search;

namespace WordLight
{
    public class UpdateRectangle
    {
        private const int MaxScreenWidth = 10000;

        private IntPtr _hWnd;
        private TextView _view;
        private object _updateRectSync = new object();

        private int _start = int.MaxValue;
        private int _end = int.MinValue;

        public UpdateRectangle(IntPtr hWnd, TextView view)
        {
            _hWnd = hWnd;
            _view = view;
        }

        public void Include(TextMark mark)
        {
            lock (_updateRectSync)
            {
                if (mark.Start < _start)
                    _start = mark.Start;

                if (mark.End > _end)
                    _end = mark.End;
            }
        }

        public void Validate()
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

        public void Invalidate()
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

        private Rectangle GetRect(int start, int end)
        {
            start = Math.Max(start, _view.VisibleTextStart);
            end = Math.Min(end, _view.VisibleTextEnd);

            var rect = _view.GetRectangle(new TextMark(start, end - start));
			if (rect != Rectangle.Empty)
			{
				rect.X = 0;
                rect.Width = MaxScreenWidth;
			}
            return rect;
        }
    }
}

