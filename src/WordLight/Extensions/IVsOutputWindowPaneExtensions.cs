using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace WordLight.Extensions
{
	public static class IVsOutputWindowPaneExtensions
    {
		public static void WriteLine(this IVsOutputWindowPane pane, string text)
        {
			pane.OutputString(text + '\n'); 
        }
    }
}
