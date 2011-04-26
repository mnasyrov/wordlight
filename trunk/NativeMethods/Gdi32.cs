using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WordLight.NativeMethods
{
	public class Gdi32
	{
		public enum StockObjects:int
		{
			WHITE_BRUSH = 0,
			LTGRAY_BRUSH = 1,
			GRAY_BRUSH = 2,
			DKGRAY_BRUSH = 3,
			BLACK_BRUSH = 4,
			NULL_BRUSH = 5,
			HOLLOW_BRUSH = NULL_BRUSH,
			WHITE_PEN = 6,
			BLACK_PEN = 7,
			NULL_PEN = 8,
			OEM_FIXED_FONT = 10,
			ANSI_FIXED_FONT = 11,
			ANSI_VAR_FONT = 12,
			SYSTEM_FONT = 13,
			DEVICE_DEFAULT_FONT = 14,
			DEFAULT_PALETTE = 15,
			SYSTEM_FIXED_FONT = 16,
			DEFAULT_GUI_FONT = 17,
			DC_BRUSH = 18,
			DC_PEN = 19
		}

		/// <summary>
		///     Specifies a raster-operation code. These codes define how the color data for the
		///     source rectangle is to be combined with the color data for the destination
		///     rectangle to achieve the final color.
		/// </summary>
		public enum TernaryRasterOperations : uint
		{
			/// <summary>dest = source</summary>
			SRCCOPY = 0x00CC0020,
			/// <summary>dest = source OR dest</summary>
			SRCPAINT = 0x00EE0086,
			/// <summary>dest = source AND dest</summary>
			SRCAND = 0x008800C6,
			/// <summary>dest = source XOR dest</summary>
			SRCINVERT = 0x00660046,
			/// <summary>dest = source AND (NOT dest)</summary>
			SRCERASE = 0x00440328,
			/// <summary>dest = (NOT source)</summary>
			NOTSRCCOPY = 0x00330008,
			/// <summary>dest = (NOT src) AND (NOT dest)</summary>
			NOTSRCERASE = 0x001100A6,
			/// <summary>dest = (source AND pattern)</summary>
			MERGECOPY = 0x00C000CA,
			/// <summary>dest = (NOT source) OR dest</summary>
			MERGEPAINT = 0x00BB0226,
			/// <summary>dest = pattern</summary>
			PATCOPY = 0x00F00021,
			/// <summary>dest = DPSnoo</summary>
			PATPAINT = 0x00FB0A09,
			/// <summary>dest = pattern XOR dest</summary>
			PATINVERT = 0x005A0049,
			/// <summary>dest = (NOT dest)</summary>
			DSTINVERT = 0x00550009,
			/// <summary>dest = BLACK</summary>
			BLACKNESS = 0x00000042,
			/// <summary>dest = WHITE</summary>
			WHITENESS = 0x00FF0062,
			/// <summary>
			/// Capture window as seen on screen.  This includes layered windows 
			/// such as WPF windows with AllowsTransparency="true"
			/// </summary>
			CAPTUREBLT = 0x40000000
		}

		/// <summary>
		///    Performs a bit-block transfer of the color data corresponding to a
		///    rectangle of pixels from the specified source device context into
		///    a destination device context.
		/// </summary>
		/// <param name="hdc">Handle to the destination device context.</param>
		/// <param name="nXDest">The leftmost x-coordinate of the destination rectangle (in pixels).</param>
		/// <param name="nYDest">The topmost y-coordinate of the destination rectangle (in pixels).</param>
		/// <param name="nWidth">The width of the source and destination rectangles (in pixels).</param>
		/// <param name="nHeight">The height of the source and the destination rectangles (in pixels).</param>
		/// <param name="hdcSrc">Handle to the source device context.</param>
		/// <param name="nXSrc">The leftmost x-coordinate of the source rectangle (in pixels).</param>
		/// <param name="nYSrc">The topmost y-coordinate of the source rectangle (in pixels).</param>
		/// <param name="dwRop">A raster-operation code.</param>
		/// <returns>
		///    <c>true</c> if the operation succeeded, <c>false</c> otherwise.
		/// </returns>
		[DllImport("gdi32.dll")]
		public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport("gdi32.dll")]
		public static extern bool DeleteDC(IntPtr hdc);

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		[DllImport("gdi32.dll")]
        public static extern int GetClipBox(IntPtr hdc, out User32.RECT lprc);

		public static User32.RECT GetClipBox(IntPtr hdc)
		{
            var result = new User32.RECT();
			GetClipBox(hdc, out result);
			return result;
		}

		[DllImport("gdi32.dll")]
		public static extern IntPtr GetStockObject(int fnObject);

		public static IntPtr GetStockObject(StockObjects stockObject)
		{
			return GetStockObject((int)stockObject);
		}

		[DllImport("gdi32.dll")]
		public static extern bool Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

		[DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

		[DllImport("gdi32.dll")]
		public static extern int SetDCPenColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll")]
        public static extern int GetPixel(IntPtr hDC, int x, int y);

        public static Color GetPixel(Graphics g, int x, int y)
        {
            Color color = Color.Empty;
            if (g != null)
            {
                IntPtr hDC = g.GetHdc();
                int colorRef = GetPixel(hDC, x, y);
                color = Color.FromArgb(
                    (int)(colorRef & 0x000000FF),
                    (int)(colorRef & 0x0000FF00) >> 8,
                    (int)(colorRef & 0x00FF0000) >> 16);
                g.ReleaseHdc();
            }
            return color;
        }

		//[StructLayout(LayoutKind.Sequential)]
		//public struct COLORREF
		//{
		//    public int ColorDWORD;

		//    public COLORREF(Color color)
		//    {
		//        ColorDWORD = ColorTranslator.ToWin32(color);
		//    }

		//    public Color GetColor()
		//    {
		//        return ColorTranslator.FromWin32(ColorDWORD);
		//    }

		//    public void SetColor(Color color)
		//    {
		//        ColorDWORD = ColorTranslator.ToWin32(color);
		//    }
		//}
	}
}
