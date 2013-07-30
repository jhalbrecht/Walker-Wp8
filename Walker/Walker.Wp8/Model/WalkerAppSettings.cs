using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Walker.Pcl.Model;
using Walker.Wp8;

namespace Walker.Model
{
    public class WalkerAppSettings
    {
        IsolatedStorageSettings settings;
        private AdpLogger _logger;

        /// <summary>
        /// Constructor that gets the application settings.
        /// </summary>
        public WalkerAppSettings()
        {
            _logger = new AdpLogger();

            try
            {
                // Get the settings for this application.
                settings = IsolatedStorageSettings.ApplicationSettings;
            }
            catch (Exception e)
            {
                _logger.Log(this, "Exception while using IsolatedStorageSettings: ", e.ToString());
            }
        }

        const string BlueToothEnabledSettingKeyName = "BlueToothEnabledCheckBoxSetting";
        const bool BlueTooghEnabledSettingDefault = false;

        /// <summary>
        /// Property to get and set a CheckBox Setting Key.
        /// </summary>
        public bool BlueToothEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(BlueToothEnabledSettingKeyName, BlueTooghEnabledSettingDefault);
            }
            set
            {
                if (AddOrUpdateValue(BlueToothEnabledSettingKeyName, value))
                {
                    Save();
                }
            }
        }

        const string GpxEnabledSettingKeyName = "GpxEnabledCheckBoxSetting";
        const bool GpxEnabledSettingDefault = false;

        /// <summary>
        /// Property to get and set a CheckBox Setting Key.
        /// </summary>
        public bool GpxEnabled
        {
            get
            {
                return GetValueOrDefault<bool>(GpxEnabledSettingKeyName, GpxEnabledSettingDefault);
            }
            set
            {
                if (AddOrUpdateValue(GpxEnabledSettingKeyName, value))
                {
                    Save();
                }
            }
        }

        const string AzureEnabledSettingKeyName = "AzureEnabledCheckBoxSetting";
        const bool AzureEnabledSettingDefault = false;

        /// <summary>
        /// Property to get and set a CheckBox Setting Key.
        /// </summary>
        public bool AzureEnabled        {
            get
            {
                return GetValueOrDefault<bool>(AzureEnabledSettingKeyName, AzureEnabledSettingDefault);
            }
            set
            {
                if (AddOrUpdateValue(AzureEnabledSettingKeyName, value))
                {
                    Save();
                }
            }
        }


        /// <summary>
        /// Update a setting value for our application. If the setting does not
        /// exist, then add the setting.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddOrUpdateValue(string Key, Object value)
        {
            bool valueChanged = false;

            // If the key exists
            if (settings.Contains(Key))
            {
                // If the value has changed
                if (settings[Key] != value)
                {
                    // Store the new value
                    settings[Key] = value;
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                settings.Add(Key, value);
                valueChanged = true;
            }

            return valueChanged;
        }


        /// <summary>
        /// Get the current value of the setting, or if it is not found, set the 
        /// setting to the default setting.
        /// </summary>
        /// <typeparam name="valueType"></typeparam>
        /// <param name="Key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public valueType GetValueOrDefault<valueType>(string Key, valueType defaultValue)
        {
            valueType value;

            // If the key exists, retrieve the value.
            if (settings.Contains(Key))
            {
                value = (valueType)settings[Key];
            }
            // Otherwise, use the default value.
            else
            {
                value = defaultValue;
            }

            return value;
        }


        /// <summary>
        /// Save the settings.
        /// </summary>
        public void Save()
        {
            settings.Save();
        }

    }
}
