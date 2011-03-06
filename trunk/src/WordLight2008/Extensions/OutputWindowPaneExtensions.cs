using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Extensions
{
	public static class OutputWindowPaneExtensions
    {
        public static void WriteLine(this OutputWindowPane pane, string text)
        {
			pane.OutputString(text + '\n'); 
        }
    }
}
