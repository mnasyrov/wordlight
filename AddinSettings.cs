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

		#region Colors
		
		[Category("Colors")]
        [DisplayName("Search results")]
        public Color SearchMarkBorderColor { get; set; }

		[Category("Colors")]
        [DisplayName("Frozen search 1")]
        public Color FreezeMark1BorderColor { get; set; }

		[Category("Colors")]
		[DisplayName("Frozen search 2")]
        public Color FreezeMark2BorderColor { get; set; }

		[Category("Colors")]
		[DisplayName("Frozen search 3")]
        public Color FreezeMark3BorderColor { get; set; }

		#endregion

		#region Hotkeys

		[Category("Hotkeys")]
		[DisplayName("Freeze search 1"), Description("Needs restart of the add-in")]
		public string FreezeMark1Hotkey { get; set; }

		[Category("Hotkeys")]
		[DisplayName("Freeze search 2"), Description("Needs restart of the add-in")]
		public string FreezeMark2Hotkey { get; set; }

		[Category("Hotkeys")]
		[DisplayName("Freeze search 3"), Description("Needs restart of the add-in")]
		public string FreezeMark3Hotkey { get; set; }

		#endregion 
		
		#region Search

		[Category("Search")]
		[DisplayName("Match case"), Description("Needs reopening of documents")]
		public bool CaseSensitiveSearch { get; set; }

        [Category("Search")]
        [DisplayName("Match whole words"), Description("Needs reopening of documents")]
        public bool SearchWholeWordsOnly { get; set; }

		#endregion

		#region Experemental

		[Category("Experemental")]
        [DisplayName("Filled marks"), Description("Fills marks with semitransparent colors")]
        public bool FilledMarks { get; set; }

		#endregion

		protected AddinSettings()
        {
            ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            SearchMarkBorderColor = Color.FromArgb(255, 105, 180); //Hot pink

            FreezeMark1BorderColor = Color.FromArgb(0, 112, 255); //Brandeis blue (azure)
            FreezeMark2BorderColor = Color.FromArgb(76, 187, 23); //Kelly green
            FreezeMark3BorderColor = Color.FromArgb(255, 69, 0); //Orange red

            FreezeMark1Hotkey = "Global::ctrl+`";
            FreezeMark2Hotkey = "Global::ctrl+1";
            FreezeMark3Hotkey = "Global::ctrl+2";

            FilledMarks = false;

			CaseSensitiveSearch = false;
            SearchWholeWordsOnly = false;
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

				CaseSensitiveSearch = settings.GetBoolSetting("CaseSensitiveSearch", CaseSensitiveSearch);
                SearchWholeWordsOnly = settings.GetBoolSetting("SearchWholeWordsOnly", SearchWholeWordsOnly);                
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

				settings.SetBoolSetting("CaseSensitiveSearch", CaseSensitiveSearch);
                settings.SetBoolSetting("SearchWholeWordsOnly", SearchWholeWordsOnly);
            }
        }
    }    
}