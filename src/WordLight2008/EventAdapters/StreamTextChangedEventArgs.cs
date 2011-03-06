using System;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.EventAdapters
{
	public class StreamTextChangedEventArgs : EventArgs
	{
		private int _position;
		private int _oldLength;
		private int _newLength;

		public int Position
		{
			get { return _position; }
		}

		public int OldLength
		{
			get { return _oldLength; }
		}

		public int NewLength
		{
			get { return _newLength; }
		}

		public StreamTextChangedEventArgs(int position, int oldLength, int newLength)
		{
			_position = position;
			_oldLength = oldLength;
			_newLength = newLength;
		}
	}
}
