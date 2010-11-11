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
        private object _repositorySync = new object();

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
            lock (_repositorySync)
            {
                _repository = repository;
            }
            Reload();            
        }

        public void Reload()
        {
            ResetToDefaults();
            
            lock (_repositorySync)
            {
                if (_repository == null) ActivityLog.Warning("Settings repository is not set");

                if (_repository != null)
                {
                    SearchMarkBorderColor = _repository.GetColorSetting("SearchMarkBorderColor", SearchMarkBorderColor);

                    FreezeMark1BorderColor = _repository.GetColorSetting("FreezeMark1BorderColor", FreezeMark1BorderColor);
                    FreezeMark2BorderColor = _repository.GetColorSetting("FreezeMark2BorderColor", FreezeMark2BorderColor);
                    FreezeMark3BorderColor = _repository.GetColorSetting("FreezeMark3BorderColor", FreezeMark3BorderColor);

                    FreezeMark1Hotkey = _repository.GetSetting("FreezeMark1Hotkey", FreezeMark1Hotkey);
                    FreezeMark2Hotkey = _repository.GetSetting("FreezeMark2Hotkey", FreezeMark2Hotkey);
                    FreezeMark3Hotkey = _repository.GetSetting("FreezeMark3Hotkey", FreezeMark3Hotkey);

                    FilledMarks = _repository.GetBoolSetting("FilledMarks", FilledMarks);

                    CaseSensitiveSearch = _repository.GetBoolSetting("CaseSensitiveSearch", CaseSensitiveSearch);
                    SearchWholeWordsOnly = _repository.GetBoolSetting("SearchWholeWordsOnly", SearchWholeWordsOnly);
                }
            }
        }

        public void Save()
        {
            lock (_repositorySync)
            {
                if (_repository == null) ActivityLog.Warning("Settings repository is not set");

                if (_repository != null)
                {
                    _repository.SetColorSetting("SearchMarkBorderColor", SearchMarkBorderColor);

                    _repository.SetColorSetting("FreezeMark1BorderColor", FreezeMark1BorderColor);
                    _repository.SetColorSetting("FreezeMark2BorderColor", FreezeMark2BorderColor);
                    _repository.SetColorSetting("FreezeMark3BorderColor", FreezeMark3BorderColor);

                    _repository.SetSetting("FreezeMark1Hotkey", FreezeMark1Hotkey);
                    _repository.SetSetting("FreezeMark2Hotkey", FreezeMark2Hotkey);
                    _repository.SetSetting("FreezeMark3Hotkey", FreezeMark3Hotkey);

                    _repository.SetBoolSetting("FilledMarks", FilledMarks);

                    _repository.SetBoolSetting("CaseSensitiveSearch", CaseSensitiveSearch);
                    _repository.SetBoolSetting("SearchWholeWordsOnly", SearchWholeWordsOnly);
                }
            }
        }
    }    
}