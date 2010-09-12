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
        [DisplayName("Border color"), Description("Defines a color for a border of a search mark")]
        public Color SearchMarkBorderColor { get; set; }

        [Category("Freeze marks")]
        [DisplayName("Mark 1"), Description("Color of a border")]
        public Color FreezeMark1BorderColor { get; set; }

        [Category("Freeze marks")]
        [DisplayName("Mark 2"), Description("Color of a border")]
        public Color FreezeMark2BorderColor { get; set; }

        [Category("Freeze marks")]
        [DisplayName("Mark 3"), Description("Color of a border")]
        public Color FreezeMark3BorderColor { get; set; }

        protected AddinSettings()
        {
            ResetToDefaults();
        }

        private void ResetToDefaults()
        {
            SearchMarkBorderColor = Color.FromArgb(255, 105, 180); //Hot pink

            FreezeMark1BorderColor = Color.FromArgb(0, 112, 255); //Brandeis blue (azure)
            FreezeMark2BorderColor = Color.FromArgb(76, 187, 23); //Kelly green
            FreezeMark3BorderColor = Color.FromArgb(255, 69, 0); //Orange red
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
                SearchMarkBorderColor = settings.GetColorSetting("SearchMarkBorderColor", SearchMarkBorderColor);

                FreezeMark1BorderColor = settings.GetColorSetting("FreezeMark1BorderColor", FreezeMark1BorderColor);
                FreezeMark2BorderColor = settings.GetColorSetting("FreezeMark2BorderColor", FreezeMark2BorderColor);
                FreezeMark3BorderColor = settings.GetColorSetting("FreezeMark3BorderColor", FreezeMark3BorderColor);
            }
        }

        public void Save()
        {
            SettingRepository settings = _repository;
            if (settings != null)
            {
                settings.SetColorSetting("SearchMarkBorderColor", SearchMarkBorderColor);

                settings.SetColorSetting("FreezeMark1BorderColor", FreezeMark1BorderColor);
                settings.SetColorSetting("FreezeMark2BorderColor", FreezeMark2BorderColor);
                settings.SetColorSetting("FreezeMark3BorderColor", FreezeMark3BorderColor);
            }
        }
    }    
}