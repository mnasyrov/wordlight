using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;

using EnvDTE;
using EnvDTE80;

namespace WordLight
{
    public class AddinSettings
    {
        #region Singlenton
        private sealed class SingletonCreator
        {
            private static readonly AddinSettings _instance = new AddinSettings();
            public static AddinSettings Instance { get { return _instance; } }
        }

        public static AddinSettings Instance { get { return SingletonCreator.Instance; } }
           #endregion

        private SettingRepository _repository;

        [Category("Search marks")]
        [DisplayName("Outline color"), Description("Defines outline color for search marks")]
        public Color SearchMarkOutlineColor { get; set; }

        protected AddinSettings()
        {
            ResetToDefaults();
        }

        private void ResetToDefaults()
        {
            SearchMarkOutlineColor = Color.Orange;            
        }

        public void Load(SettingRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            _repository = repository;
            Reload();            
        }

        public void Reload()
        {
            ResetToDefaults();
            
            SettingRepository settings = _repository;
            if (settings != null)
            {
                SearchMarkOutlineColor = settings.GetColorSetting("SearchMarkOutlineColor", SearchMarkOutlineColor);
            }
        }

        public void Save()
        {
            SettingRepository settings = _repository;
            if (settings != null)
            {
                settings.SetColorSetting("SearchMarkOutlineColor", SearchMarkOutlineColor);
            }
        }
    }    
}