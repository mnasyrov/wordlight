using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace WordLight
{
    public interface IScreenUpdateManager
    {
		void IncludeText(int position, int length);
        void CompleteUpdate();
        void RequestUpdate();
    }
}

