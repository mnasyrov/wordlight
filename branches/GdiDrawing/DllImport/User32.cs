using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WordLight.DllImport
{
	public class User32
	{
        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }


		[DllImport("user32.dll")]
		public static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

		[DllImport("user32.dll")]
		public static extern IntPtr EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

		[DllImport("user32.dll")]
		public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		public static RECT GetClientRect(IntPtr hWnd)
		{
			RECT result = new RECT();
			GetClientRect(hWnd, out result);
			return result;
		}

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool GetUpdateRect(IntPtr hWnd, IntPtr rect, bool erase);

		[DllImport("user32.dll")]
		public static extern bool InvalidateRect(IntPtr hWnd, IntPtr rect, bool erase);

		[DllImport("user32.dll")]
		public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("user32.dll")]
		public static extern bool UpdateWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool ValidateRect(IntPtr hWnd, IntPtr rect);

	}
}
