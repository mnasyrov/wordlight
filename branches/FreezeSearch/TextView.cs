using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.DllImport;
using WordLight.EventAdapters;
using WordLight.Extensions;
using WordLight.Search;

namespace WordLight
{
    public class TextView
    {
        private class TextPoint
        {
			public int Line;
			public int Column;

			public override int GetHashCode()
			{
				return Line.GetHashCode() ^ Column.GetHashCode();
			}
        }

        private IVsTextView _view;
        private IVsTextLines _buffer;
        private int _lineHeight;

        private Dictionary<TextPoint, Point> _pointCache = new Dictionary<TextPoint, Point>();
        private object _pointCacheSync = new object();

        public IVsTextView View
        {
            get { return _view; }
        }

        public IVsTextLines Buffer
        {
            get { return _buffer; }
        }

        public int LineHeight
        {
            get { return _lineHeight; }
        }

        public TextView(IVsTextView view)
        {
            if (view == null) throw new ArgumentNullException("view");

            _view = view;
            _buffer = view.GetBuffer();
            _lineHeight = _view.GetLineHeight();
        }

        public Point GetPointOfLineColumn(int line, int column)
        {
            var textPos = new TextPoint{Line = line, Column = column};
            var screenPoint = Point.Empty;
            
            lock (_pointCacheSync)
            {
                if (_pointCache.ContainsKey(textPos))
                {
                    screenPoint = _pointCache[textPos];
                }
                else
                {
                    var p = new Microsoft.VisualStudio.OLE.Interop.POINT[1];
                    _view.GetPointOfLineColumn(line, column, p);

                    screenPoint.X = p[0].x;
                    screenPoint.Y = p[0].y;

                    _pointCache.Add(textPos, screenPoint);
                }
            }

            return screenPoint;
        }

        public void ResetPointCache()
        {
            lock (_pointCacheSync)
            {
                _pointCache.Clear();
            }
        }
    }
}
