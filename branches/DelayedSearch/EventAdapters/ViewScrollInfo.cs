using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.EventAdapters
{
	public struct ViewScrollInfo : IEquatable<ViewScrollInfo>
	{
		public int bar;
		public int minUnit;
		public int maxUnit;
		public int visibleUnits;
		public int firstVisibleUnit;

		public bool IsHorizontal
		{
			get { return bar == 0; }
		}

		public bool IsVertical
		{
			get { return bar == 1; }
		}

		public static ViewScrollInfo Empty = new ViewScrollInfo()
		{
			bar = 0,
			firstVisibleUnit = 0,
			maxUnit = 0,
			minUnit = 0,
			visibleUnits = 0
		};

		public static ViewScrollInfo CreateByView(IVsTextView view, int iBar)
		{
			ViewScrollInfo info = new ViewScrollInfo();
			info.bar = iBar;
			view.GetScrollInfo(iBar, out info.minUnit, out info.maxUnit, out info.visibleUnits, out info.firstVisibleUnit);
			return info;
		}

		public bool Equals(ViewScrollInfo other)
		{
			return
				bar == other.bar &&
				minUnit == other.minUnit &&
				maxUnit == other.maxUnit &&
				visibleUnits == other.visibleUnits &&
				firstVisibleUnit == other.firstVisibleUnit;
		}
	}
}
