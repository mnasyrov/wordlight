using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace WordLight.Search
{
    public class TextMark
    {
        public int Position { get; set; }
		public int Length { get; set; }

		public int End
		{
			get { return Position + Length; }
		}

		public TextMark(int position, int length)
		{
			this.Position = position;
			this.Length = length;
		}
    }
}
