using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WordLight.NativeMethods
{
	public static partial class User32
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			int _left;
			int _top;
			int _right;
			int _bottom;

			#region Properties

			public int X
			{
				get { return Left; }
				set { Left = value; }
			}

			public int Y
			{
				get { return Top; }
				set { Top = value; }
			}

			public int Left
			{
				get { return _left; }
				set { _left = value; }
			}

			public int Top
			{
				get { return _top; }
				set { _top = value; }
			}

			public int Right
			{
				get { return _right; }
				set { _right = value; }
			}

			public int Bottom
			{
				get { return _bottom; }
				set { _bottom = value; }
			}

			public int Height
			{
				get { return Bottom - Top; }
				set { Bottom = value - Top; }
			}

			public int Width
			{
				get { return Right - Left; }
				set { Right = value + Left; }
			}

			public System.Drawing.Point Location
			{
				get
				{
					return new System.Drawing.Point(Left, Top);
				}
				set
				{
					Left = value.X;
					Top = value.Y;
				}
			}
			public System.Drawing.Size Size
			{
				get
				{
					return new System.Drawing.Size(Width, Height);
				}
				set
				{
					Right = value.Width + Left;
					Bottom = value.Height + Top;
				}
			}

			#endregion

			public RECT(System.Drawing.Rectangle rectangle)
				: this(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom)
			{
				//Do nothing
			}

			public RECT(int left, int top, int right, int bottom)
			{
				_left = left;
				_top = top;
				_right = right;
				_bottom = bottom;
			}
			
			public System.Drawing.Rectangle ToRectangle()
			{
				return new System.Drawing.Rectangle(this.Left, this.Top, this.Width, this.Height);
			}

			//public static System.Drawing.Rectangle ToRectangle(RECT Rectangle)
			//{
			//    return Rectangle.ToRectangle();
			//}

			public static RECT FromRectangle(System.Drawing.Rectangle Rectangle)
			{
				return new RECT(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
			}

			//public static implicit operator System.Drawing.Rectangle(RECT Rectangle)
			//{
			//    return Rectangle.ToRectangle();
			//}

			//public static implicit operator RECT(System.Drawing.Rectangle Rectangle)
			//{
			//    return new RECT(Rectangle);
			//}

			public static bool operator ==(RECT Rectangle1, RECT Rectangle2)
			{
				return Rectangle1.Equals(Rectangle2);
			}
			public static bool operator !=(RECT Rectangle1, RECT Rectangle2)
			{
				return !Rectangle1.Equals(Rectangle2);
			}

			public override string ToString()
			{
				return "{Left: " + Left + "; " + "Top: " + Top + "; Right: " + Right + "; Bottom: " + Bottom + "}";
			}

			public bool Equals(RECT Rectangle)
			{
				return Rectangle.Left == Left && Rectangle.Top == Top && Rectangle.Right == Right && Rectangle.Bottom == Bottom;
			}

			public override bool Equals(object Object)
			{
				if (Object is RECT)
				{
					return Equals((RECT)Object);
				}
				else if (Object is Rectangle)
				{
					return Equals(new RECT((System.Drawing.Rectangle)Object));
				}

				return false;
			}

			public override int GetHashCode()
			{
				return Left.GetHashCode() ^ Right.GetHashCode() ^ Top.GetHashCode() ^ Bottom.GetHashCode();
			}
		}
	}
}
