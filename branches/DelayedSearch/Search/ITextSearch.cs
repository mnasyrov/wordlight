using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

namespace WordLight.Searchers
{
    public interface ITextSearch
    {
        event EventHandler<SearchCompletedEventArgs> SearchCompleted;
        void SearchAsync(IVsTextLines buffer, string text, TextSpan startRange);
    }
}
