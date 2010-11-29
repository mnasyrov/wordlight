using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

using WordLight.EventAdapters;

namespace WordLight
{
	public interface ITextView
	{
		IVsTextLines Buffer { get; }
		TextStreamEventAdapter TextStreamEvents { get; }		
		int VisibleTextStart { get; }
		int VisibleTextEnd { get; }
		int LineHeight { get; }

		Rectangle GetRectangleForMark(int markStart, int markLength);
		IScreenUpdateManager GetScreenUpdater();
	}
}
