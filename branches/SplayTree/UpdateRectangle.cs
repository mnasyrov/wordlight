using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using WordLight.DllImport;

namespace WordLight
{
    public class UpdateRectangle
    {
        private IntPtr _hWnd;
        private Rectangle _updateRect = Rectangle.Empty;
        private object _updateRectSync = new object();

        public Rectangle Rect
        {
            get { return _updateRect; }
        }

        public UpdateRectangle(IntPtr hWnd)
        {
            _hWnd = hWnd;
        }

        public void IncludeRectangle(Rectangle rect)
        {
            if (rect == Rectangle.Empty)
                return;

            lock (_updateRectSync)
            {
                if (_updateRect == Rectangle.Empty)
                {
                    _updateRect = rect;
                }
                else
                {
                    _updateRect = Rectangle.FromLTRB(
                        Math.Min(_updateRect.Left, rect.Left),
                        Math.Min(_updateRect.Top, rect.Top),
                        Math.Max(_updateRect.Right, rect.Right),
                        Math.Max(_updateRect.Bottom, rect.Bottom)
                    );
                }
            }
        }

        public void Validate()
        {
            lock (_updateRectSync)
            {
                if (_updateRect != Rectangle.Empty)
                {
                    User32.ValidateRect(_hWnd, _updateRect);
                    _updateRect = Rectangle.Empty;
                }
            }
        }

        public void Invalidate()
        {
            lock (_updateRectSync)
            {
                if (_updateRect != Rectangle.Empty)
                {
                    User32.InvalidateRect(_hWnd, _updateRect, false);
                    _updateRect = Rectangle.Empty;
                }
            }
        }

        public void Clear()
        {
            lock (_updateRectSync)
            {
                if (_updateRect != Rectangle.Empty)
                {
                    _updateRect = Rectangle.Empty;
                }
            }
        }
    }
}
