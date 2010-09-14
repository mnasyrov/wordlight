using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WordLight.DllImport
{
	public class WinProcMessages
	{
		public const int WM_KEYDOWN = 0x0100;
		public const int WM_KEYUP = 0x101;
		public const int WM_LBUTTONUP = 0x202;
		public const int WM_RBUTTONUP = 0x205;
		public const int WM_MBUTTONUP = 0x208;
		public const int WM_XBUTTONUP = 0x20C;
		public const int WM_LBUTTONDOWN = 0x201;
		public const int WM_RBUTTONDOWN = 0x204;
		public const int WM_MBUTTONDOWN = 0x207;
		public const int WM_XBUTTONDOWN = 0x20B;
		public const int WM_LBUTTONDBLCLK = 0x0203;
		public const int WM_MBUTTONDBLCLK = 0x0209;
		public const int WM_RBUTTONDBLCLK = 0x0206;
		public const int WM_XBUTTONDBLCLK = 0x020D;
		public const int WM_PARENTNOTIFY = 0x0210;

		public const int WM_PAINT = 0x000F;
		public const int WM_ERASEBKGND = 0x0014;

		public const int WM_HSCROLL = 0x0114;
		public const int WM_VSCROLL = 0x0115;

        public const int WM_SETREDRAW = 0x000B;
	}
}
