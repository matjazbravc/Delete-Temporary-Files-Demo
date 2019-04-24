using System;
using System.ComponentModel;
using System.Configuration;

namespace DeleteTempFiles.WindowsService.Services.Helpers
{
    /// <summary>
    /// Helper class for access to app.config values
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// Generic method for reading Cloud/App settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Setting name/key</param>
        /// <param name="defaultValue"></param>
        /// <returns>Result T</returns>
        public static T GetSetting<T>(string key, T defaultValue = default(T))
        {
            var result = defaultValue;
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return result;
            }
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                result = (T)converter.ConvertFromInvariantString(value);
            }
            catch (Exception)
            {
                result = defaultValue;
            }
            return result;
        }
    }
}
