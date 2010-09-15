using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace WordLight.Search
{
    public class TextMark : IComparable<TextMark>
    {
        public int Start { get; set; }
		public int Length { get; set; }

		public int End
		{
			get { return Start + Length; }
		}

		public TextMark(int position, int length)
		{
			this.Start = position;
			this.Length = length;
		}

        public int CompareTo(TextMark other)
        {
            return Start.CompareTo(other.Start);
        }
    }
}
