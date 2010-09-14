using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WordLight.DllImport
{
	public static partial class User32
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
		public static extern bool GetUpdateRect(IntPtr hWnd, out RECT lpRect, bool erase);

		public static RECT GetUpdateRect(IntPtr hWnd, bool erase)
		{
			RECT result = new RECT();
			GetUpdateRect(hWnd, out result, erase);
			return result;
		}

		[DllImport("user32.dll")]
		public static extern bool HideCaret(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

		[DllImport("user32.dll")]
		public static extern bool InvalidateRect(IntPtr hWnd, ref RECT lpRect, bool bErase);

		public static bool InvalidateRect(IntPtr hWnd, bool bErase)
		{
			return InvalidateRect(hWnd, IntPtr.Zero, bErase);
		}

		public static bool InvalidateRect(IntPtr hWnd, System.Drawing.Rectangle rectangle, bool bErase)
		{
			RECT rect = RECT.FromRectangle(rectangle);
			return InvalidateRect(hWnd, ref rect, bErase);
		}

		[DllImport("user32.dll")]
		public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern bool ShowCaret(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool UpdateWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool ValidateRect(IntPtr hWnd, IntPtr rect);

		[DllImport("user32.dll")]
		public static extern bool ValidateRect(IntPtr hWnd, ref RECT rect);

		public static bool ValidateRect(IntPtr hWnd, System.Drawing.Rectangle rectangle)
		{
			RECT rect = RECT.FromRectangle(rectangle);
			return ValidateRect(hWnd, ref rect);
        }

        #region Helpers

        public static void TurnOffRedrawing(IntPtr hWnd)
        {
            SendMessage(hWnd, WinProcMessages.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        public static void TurnOnRedrawing(IntPtr hWnd)
        {
            SendMessage(hWnd, WinProcMessages.WM_SETREDRAW, (IntPtr) 1, IntPtr.Zero);
        }

        #endregion
    }
}
