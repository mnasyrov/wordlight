using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using WordLight;

namespace WordLight2010
{
	/// <summary>
	/// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
	/// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
	/// </summary>
	[Export(typeof(IWpfTextViewCreationListener))]
	[ContentType("text")]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	internal sealed class MarkAdornmentFactory : IWpfTextViewCreationListener
	{
		/// <summary>
		/// Defines the adornment layer for the adornment. This layer is ordered 
		/// after the selection layer in the Z-order
		/// </summary>
		[Export(typeof(AdornmentLayerDefinition))]
		[Name("MarkAdornment")]
		[Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
		[TextViewRole(PredefinedTextViewRoles.Document)]
		public AdornmentLayerDefinition editorAdornmentLayer = null;

		private static Dictionary<IWpfTextView, MarkAdornment> _adornments =
			new Dictionary<IWpfTextView, MarkAdornment>();
		private static object _adornmentSync = new object();

		public void TextViewCreated(IWpfTextView textView)
		{
			try
			{
				var adorment = new MarkAdornment(textView);

				lock (_adornmentSync)
				{
					_adornments[textView] = adorment;
				}

				textView.Closed += new EventHandler(textView_Closed);
			}
			catch (Exception ex)
			{
				Log.Error("Failed to create MarkAdornment", ex);
			}
		}

		private void textView_Closed(object sender, EventArgs e)
		{
			var textView = sender as IWpfTextView;
			if (textView != null)
			{
				lock (_adornmentSync)
				{
					_adornments.Remove(textView);
				}
			}
		}

		public static MarkAdornment FindMarkAdorment(IWpfTextView textView)
		{
			MarkAdornment adornment = null;

			if (textView != null)
			{
				lock (_adornmentSync)
				{
					_adornments.TryGetValue(textView, out adornment);
				}
			}

			return adornment;
		}
	}
}
