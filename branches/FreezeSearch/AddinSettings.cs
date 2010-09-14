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


        [Category("Hot keys")]
        [DisplayName("Freeze mark 1")]
        public string FreezeMark1Hotkey { get; set; }

        [Category("Hot keys")]
        [DisplayName("Freeze mark 2")]
        public string FreezeMark2Hotkey { get; set; }

        [Category("Hot keys")]
        [DisplayName("Freeze mark 3")]
        public string FreezeMark3Hotkey { get; set; }


        [Category("Experemental")]
        [DisplayName("Filled marks"), Description("Fills marks with semitransparent color")]
        public bool FilledMarks { get; set; }


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

            FreezeMark1Hotkey = "Global::ctrl+`";
            FreezeMark2Hotkey = "Global::ctrl+1";
            FreezeMark3Hotkey = "Global::ctrl+2";

            FilledMarks = false;
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

                FreezeMark1Hotkey = settings.GetSetting("FreezeMark1Hotkey", FreezeMark1Hotkey);
                FreezeMark2Hotkey = settings.GetSetting("FreezeMark2Hotkey", FreezeMark2Hotkey);
                FreezeMark3Hotkey = settings.GetSetting("FreezeMark3Hotkey", FreezeMark3Hotkey);

                FilledMarks = settings.GetBoolSetting("FilledMarks", FilledMarks);
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

                settings.SetSetting("FreezeMark1Hotkey", FreezeMark1Hotkey);
                settings.SetSetting("FreezeMark2Hotkey", FreezeMark2Hotkey);
                settings.SetSetting("FreezeMark3Hotkey", FreezeMark3Hotkey);

                settings.SetBoolSetting("FilledMarks", FilledMarks);
            }
        }
    }    
}