using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Searchers
{
    public class SearchJob
    {
        public IVsTextLines Buffer { get; set; }
        public string Text { get; set; }
        public TextSpan Range { get; set; }
    }
}
