using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using EnvDTE;
using EnvDTE80;
using WordLight.Common;

namespace WordLight
{
    public class WordLightSettings: AddinSettings
    {
        public WordLightSettings(Globals globals) :
            base(globals, "WordLight")
        {
            // Do nohting
        }

        public Color SearchMarkOutlineColor
        {
            get { return GetColorSetting("SearchMarkOutlineColor", Color.BlueViolet); }
            set { SetColorSetting("SearchMarkOutlineColor", value); }
        }
    }    
}
