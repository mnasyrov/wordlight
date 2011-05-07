using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using WordLight;
using WordLight.NativeMethods;
using WordLight.Search;

namespace WordLight2010
{
	public class ScreenUpdateManager : IScreenUpdateManager
    {
        private const int MaxScreenWidth = 10000;

        private TextView _view;
        private object _updateRectSync = new object();

        private int _start = int.MaxValue;
        private int _end = int.MinValue;

        public ScreenUpdateManager(TextView view)
        {
            if (view == null) throw new ArgumentNullException("view");
            _view = view;
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
                    if (rect != Rectangle.Empty && _view.WindowHandle != IntPtr.Zero)
                        User32.ValidateRect(_view.WindowHandle, rect);

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
                    if (rect != Rectangle.Empty && _view.WindowHandle != IntPtr.Zero)
                        User32.InvalidateRect(_view.WindowHandle, rect, false);
                }
            }
        }

        private Rectangle GetRect(int textStart, int textEnd)
        {
            textStart = Math.Max(textStart, _view.VisibleTextStart);
            textEnd = Math.Min(textEnd, _view.VisibleTextEnd);

            var rect = _view.GetRectangleForMark(textStart, textEnd - textStart);
			if (rect != Rectangle.Empty)
			{
				rect.X = 0;
                rect.Width = MaxScreenWidth;
			}
            return rect;
        }
    }
}

