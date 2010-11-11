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
            try
            {
                key = ExtendKey(key);
                if (_globals.get_VariableExists(key))
                {
                    return (string)_globals[key];
                }
            }
            catch (Exception ex)
            {
                ActivityLog.Error(string.Format("Failed to load a setting by key '{0}'", key), ex);
            }
            return null;
        }

        public void SetSetting(string key, string value)
        {
            try
            {
                key = ExtendKey(key);
                _globals[key] = value;
                _globals.set_VariablePersists(key, true);
            }
            catch (Exception ex)
            {
                ActivityLog.Error(string.Format("Failed to store a setting by key '{0}' and value '{1}'", key, value), ex);
            }
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
