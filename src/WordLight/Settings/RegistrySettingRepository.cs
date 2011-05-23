using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using Microsoft.Win32;

using EnvDTE;
using EnvDTE80;

namespace WordLight.Settings
{
    public class RegistrySettingRepository: SettingRepository
    {
		private RegistryKey _settingsKey;

		public RegistrySettingRepository(string registryKeyPath)
        {
			if (string.IsNullOrEmpty(registryKeyPath)) throw new ArgumentException("registryKeyPath");

			try
			{
				_settingsKey = Registry.CurrentUser.CreateSubKey(registryKeyPath);
			}
			catch (Exception ex)
			{
				Log.Error(string.Format("Failed to initialize a setting repository by path '{0}'", registryKeyPath), ex);
			}
        }

		public RegistrySettingRepository(RegistryKey settingsKey)
		{
			if (settingsKey == null) throw new ArgumentNullException("settingsKey");

			_settingsKey = settingsKey;
		}

        public override string GetSetting(string key)
        {
            try
            {
				return (string)_settingsKey.GetValue(key);
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
				_settingsKey.SetValue(key, value);
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Failed to store a setting by key '{0}' and value '{1}'", key, value), ex);
            }
        }
    }
}
