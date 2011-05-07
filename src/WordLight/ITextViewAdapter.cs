using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using WordLight.EventAdapters;

namespace WordLight
{
	public interface ITextViewAdapter
	{
		int VisibleTextStart { get; }
		int VisibleTextEnd { get; }
		TextStreamEventAdapter TextStreamEvents { get; }
		IScreenUpdateManager ScreenUpdater { get; }
		int LineHeight { get; }

		string GetBufferText();
		Rectangle GetRectangleForMark(int markStart, int markLength);
	}
}
