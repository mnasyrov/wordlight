using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using EnvDTE;
using EnvDTE80;

namespace WordLight.Common
{
    public class AddinSettings
    {
        private Globals _globals;
        private string _keyPrefix;

        public AddinSettings(Globals globals, string keyPrefix)
        {
            if (globals == null) throw new ArgumentNullException("globals");
            _globals = globals;
            _keyPrefix = keyPrefix;
        }

        private string ExtendKey(string key)
        {
            if (string.IsNullOrEmpty(_keyPrefix))
                return key;
            return _keyPrefix + key;
        }

        protected string GetSetting(string key)
        {
            key = ExtendKey(key);
            if (_globals.get_VariableExists(key))
            {
                return (string)_globals[key];
            }
            return null;
        }

        protected void SetSetting(string key, string value)
        {
            key = ExtendKey(key);
            _globals[key] = value;
            _globals.set_VariablePersists(key, true);
        }
        
        protected string GetSetting(string key, string defaultValue)
        {
            string value = GetSetting(key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return value;
        }

        protected Color GetColorSetting(string key, Color defaultColor)
        {
            string value = GetSetting(key);

            int argb;
            if (int.TryParse(value, out argb))
            {
                return Color.FromArgb(argb);
            }
            return defaultColor;
        }

        protected void SetColorSetting(string key, Color value)
        {
            SetSetting(key, value.ToArgb().ToString());
        }
    }
}
