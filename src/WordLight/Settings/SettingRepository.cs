using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

using EnvDTE;
using EnvDTE80;

namespace WordLight.Settings
{
    public abstract class SettingRepository
    {
        public abstract string GetSetting(string key);
        public abstract void SetSetting(string key, string value);

        public virtual string GetSetting(string key, string defaultValue)
        {
            string value = GetSetting(key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return value;
        }

        public virtual Color GetColorSetting(string key, Color defaultColor)
        {
            string value = GetSetting(key);

            int argb;
            if (int.TryParse(value, out argb))
            {
                return Color.FromArgb(argb);
            }
            return defaultColor;
        }

        public virtual bool GetBoolSetting(string key, bool defaultValue)
        {
            string value = GetSetting(key);

            bool parsedValue;
            if (bool.TryParse(value, out parsedValue))
            {
                return parsedValue;
            }
            return defaultValue;
        }

        public virtual void SetColorSetting(string key, Color value)
        {
            SetSetting(key, value.ToArgb().ToString(CultureInfo.InvariantCulture));
        }

        public virtual void SetBoolSetting(string key, bool value)
        {
            SetSetting(key, value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
