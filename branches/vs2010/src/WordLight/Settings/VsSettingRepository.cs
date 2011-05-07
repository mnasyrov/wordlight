using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

using EnvDTE;
using EnvDTE80;

namespace WordLight.Settings
{
    public class VsSettingRepository: SettingRepository
    {
        private Globals _globals;
        private string _keyPrefix;

        public VsSettingRepository(Globals globals, string addinName)
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

        public override string GetSetting(string key)
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
                Log.Error(string.Format("Failed to load a setting by key '{0}'", key), ex);
            }
            return null;
        }

        public override void SetSetting(string key, string value)
        {
            try
            {
                key = ExtendKey(key);
                _globals[key] = value;
                _globals.set_VariablePersists(key, true);
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Failed to store a setting by key '{0}' and value '{1}'", key, value), ex);
            }
        }
    }
}
