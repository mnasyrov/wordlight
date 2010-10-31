using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

using EnvDTE;
using EnvDTE80;

namespace WordLight
{
    public class SettingRepository
    {
        private Globals _globals;
        private string _keyPrefix;

        public SettingRepository(Globals globals, string addinName)
        {
            if (globals == null) throw new ArgumentNullException("globals");
            if (string.IsNullOrEmpty(addinName)) throw new ArgumentNullException("addinName");
            
            _globals = globals;
            _keyPrefix = addinName + '_';
        }

        private string ExtendKey(string key)
        {
            if (string.IsNullOrEmpty(_keyPrefix))
                return key;
            return _keyPrefix + key;
        }

        public string GetSetting(string key)
        {
            key = ExtendKey(key);
            if (_globals.get_VariableExists(key))
            {
                return (string)_globals[key];
            }
            return null;
        }

        public void SetSetting(string key, string value)
        {
            key = ExtendKey(key);
            _globals[key] = value;
            _globals.set_VariablePersists(key, true);
        }

        public string GetSetting(string key, string defaultValue)
        {
            string value = GetSetting(key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return value;
        }

        public Color GetColorSetting(string key, Color defaultColor)
        {
            string value = GetSetting(key);

            int argb;
            if (int.TryParse(value, out argb))
            {
                return Color.FromArgb(argb);
            }
            return defaultColor;
        }

        public bool GetBoolSetting(string key, bool defaultValue)
        {
            string value = GetSetting(key);

            bool parsedValue;
            if (bool.TryParse(value, out parsedValue))
            {
                return parsedValue;
            }
            return defaultValue;
        }

        public void SetColorSetting(string key, Color value)
        {
            SetSetting(key, value.ToArgb().ToString(CultureInfo.InvariantCulture));
        }

        public void SetBoolSetting(string key, bool value)
        {
            SetSetting(key, value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
