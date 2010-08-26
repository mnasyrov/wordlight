using System;
using System.Collections.Generic;
using System.Text;

using EnvDTE;
using EnvDTE80;
using Extensibility;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.EventAdapters
{
	public class TextStreamEventAdapter : IVsTextStreamEvents, IDisposable
	{
		private uint _connectionCookie;
		private IConnectionPoint _connectionPoint;

		public event EventHandler StreamTextChanged;

		public TextStreamEventAdapter(IVsTextBuffer buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");

			var cpContainer = buffer as IConnectionPointContainer;
			Guid riid = typeof(IVsTextStreamEvents).GUID;
			cpContainer.FindConnectionPoint(ref riid, out _connectionPoint);

			_connectionPoint.Advise(this, out _connectionCookie);
		}

		public void Dispose()
		{
			if (_connectionCookie > 0)
			{
				_connectionPoint.Unadvise(_connectionCookie);
				_connectionCookie = 0;
			}
		}

		/// <summary>
		/// Notifies the clients when the content of a text stream in the buffer has changed.
		/// </summary>
		/// <param name="iPos">Starting position of the affected text.</param>
		/// <param name="iOldLen">Previous length of text.</param>
		/// <param name="iNewLen">New length of text.</param>
		/// <param name="fLast">Obsolete; ignore.</param>
		public void OnChangeStreamText(int iPos, int iOldLen, int iNewLen, int fLast)
		{
			//Make a basic notification only.
			EventHandler evt = StreamTextChanged;
			if (evt != null)
				evt(this, EventArgs.Empty);
		}

		/// <summary>
		/// Notifies the client that the text stream attributes have changed.
		/// </summary>
		/// <param name="iPos">Starting position of the affected text.</param>
		/// <param name="iLength">Length of the text affected in the text stream.</param>
		public void OnChangeStreamAttributes(int iPos, int iLength)
		{
			//Not used
		}
	}
}
