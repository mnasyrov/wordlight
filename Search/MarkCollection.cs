using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Search
{
    public class MarkCollection
    {
        private object _marksSyncRoot = new object();
        private ScreenUpdateManager _screenUpdater;
        private int _markLength;
        private Treap _positions;

        public MarkCollection(ScreenUpdateManager screenUpdater)
        {
            if (screenUpdater == null) throw new ArgumentNullException("screenUpdater");

            _screenUpdater = screenUpdater;
        }

        public void Clear()
        {
            lock (_marksSyncRoot)
            {
                if (_positions != null)
                {
                    _positions.ForEachInOrder((x) => 
                    {
                        _screenUpdater.IncludeText(x, _markLength);
                    });
                    _positions = null;
                    _markLength = 0;
                }
            }
        }

        public void ReplaceMarks(TextOccurences occurences)
        {
            if (occurences == null) throw new ArgumentNullException("occurences");

            lock (_marksSyncRoot)
            {
                if (_positions != null)
                {
                    _positions.ForEachInOrder((x) =>
                    {
                        _screenUpdater.IncludeText(x, _markLength);
                    });
                    _positions = null;
                }

                if (occurences != TextOccurences.Empty && occurences.Count > 0)
                {
                    _markLength = occurences.TextLength;

                    occurences.Positions.ForEachInOrder((x) =>
                    {
                        _screenUpdater.IncludeText(x, occurences.TextLength);
                    });

                    _positions = occurences.Positions;
                }
            }
        }

        public void AddMarks(TextOccurences occurences)
        {
            if (occurences == null) throw new ArgumentNullException("occurences");

            lock (_marksSyncRoot)
            {
                if (occurences != TextOccurences.Empty && occurences.Count > 0)
                {
                    _markLength = occurences.TextLength;
                    
                    var n = occurences.Positions;
                    if (n != null)
                    {
                        if (_positions != null)
                        {
                            n.ForEachLessThan(_positions.GetMinX(), (x) => 
                            {
                                _screenUpdater.IncludeText(x, _markLength); 
                            });
                            n.ForEachGreaterThan(_positions.GetMaxX(), (x) => 
                            {
                                _screenUpdater.IncludeText(x, _markLength); 
                            });
                        }
                        else
                        {
                            n.ForEachInOrder((x) => {
                                _screenUpdater.IncludeText(x, _markLength);
                            });
                        }
                    }

                    _positions = n;
                }
            }
        }

        private Treap FindMarkThatContainsPosition(Treap node, int pos)
        {
            if (node != null)
            {
                if (node.x <= pos && pos <= node.x + _markLength)
                    return node;
                if (pos < node.x)
                    return FindMarkThatContainsPosition(node.Left, pos);
                if (pos > node.x)
                    return FindMarkThatContainsPosition(node.Right, pos);
            }
            return null;
        }

        public void ReplaceMarks(TextOccurences occurences, int start, int end, int tailOffset)
        {
            if (occurences == null) throw new ArgumentNullException("occurences");

            lock (_marksSyncRoot)
            {
                if (_positions == null)
                {
                    ReplaceMarks(occurences);
                    return;
                }

                Treap markThatContainsStart = FindMarkThatContainsPosition(_positions, start);
                if (markThatContainsStart != null)
                {
                    start = markThatContainsStart.x;
                }

                Treap right, garbage;
                _positions.Split(start - 1, out _positions, out right);
                right.Split(end, out garbage, out right);

                if (garbage != null)
                    garbage.ForEachInOrder((x) => {
                        _screenUpdater.IncludeText(x, _markLength); 
                    });

                if (occurences != TextOccurences.Empty && occurences.Count > 0)
                {
                    occurences.Positions.ForEachInOrder((x) =>
                    {
                        _screenUpdater.IncludeText(x, occurences.TextLength);
                    });

                    _markLength = occurences.TextLength;
                    _positions = Treap.Merge(_positions, occurences.Positions);
                }

                if (right != null)
                {
                    TreapBuilder shiftedMarks = new TreapBuilder();
                    right.ForEachInOrder((x) => { shiftedMarks.Add(x + tailOffset); });
                    _positions = Treap.Merge(_positions, shiftedMarks.ToTreap());
                }
            }
        }

        public Rectangle[] GetRectanglesForVisibleMarks(TextView view, Rectangle clip)
        {
            List<Rectangle> rectList = null;

            lock (_marksSyncRoot)
            {
                if (_positions != null)
                {
                    _positions.ForEachInOrderBetween(
                        view.VisibleTextStart - _markLength,
                        view.VisibleTextEnd + _markLength,
                        (x) =>
                        {
                            Rectangle rect = view.GetRectangleForMark(x, _markLength);
                            if (rect != Rectangle.Empty)
                            {
                                rect.Width -= 1;
                                rect.Height -= 1;

                                var intersectedRect = Rectangle.Intersect(clip, rect);
                                if (intersectedRect != Rectangle.Empty)
                                {
                                    if (rectList == null)
                                        rectList = new List<Rectangle>();
                                    rectList.Add(intersectedRect);
                                }
                            }
                        }
                    );
                }
            }

            if (rectList != null)
                return rectList.ToArray();

            return null;
        }
    }
}
