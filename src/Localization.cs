using System;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace AgentSupervisor
{
    /// <summary>
    /// Manages application localization and resource strings
    /// </summary>
    public static class Localization
    {
        private static ResourceManager? _resourceManager;
        private static CultureInfo _currentCulture;

        static Localization()
        {
            // Initialize resource manager
            _resourceManager = new ResourceManager("AgentSupervisor.src.Resources.Strings", typeof(Localization).Assembly);
            
            // Load saved language preference or use system default
            var savedLanguage = Configuration.LoadLanguage();
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                try
                {
                    _currentCulture = CultureInfo.GetCultureInfo(savedLanguage);
                }
                catch
                {
                    _currentCulture = GetDefaultCulture();
                }
            }
            else
            {
                _currentCulture = GetDefaultCulture();
            }
            
            ApplyCulture(_currentCulture);
        }

        /// <summary>
        /// Gets the default culture based on system settings
        /// </summary>
        private static CultureInfo GetDefaultCulture()
        {
            var systemCulture = CultureInfo.CurrentUICulture;
            
            // Check if system culture is Chinese
            if (systemCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.GetCultureInfo("zh-CN");
            }
            
            // Default to English
            return CultureInfo.GetCultureInfo("en");
        }

        /// <summary>
        /// Gets a localized string by key
        /// </summary>
        public static string GetString(string key)
        {
            try
            {
                var value = _resourceManager?.GetString(key, _currentCulture);
                return value ?? key; // Return key if not found
            }
            catch
            {
                return key;
            }
        }

        /// <summary>
        /// Gets a formatted localized string by key with parameters
        /// </summary>
        public static string GetString(string key, params object[] args)
        {
            try
            {
                var format = GetString(key);
                return string.Format(format, args);
            }
            catch
            {
                return key;
            }
        }

        /// <summary>
        /// Sets the current application culture
        /// </summary>
        public static void SetCulture(string cultureName)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureName);
                _currentCulture = culture;
                ApplyCulture(culture);
                
                // Save the language preference
                Configuration.SaveLanguage(cultureName);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to set culture to {cultureName}", ex);
            }
        }

        /// <summary>
        /// Applies the culture to the current thread
        /// </summary>
        private static void ApplyCulture(CultureInfo culture)
        {
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
        }

        /// <summary>
        /// Gets the current culture name
        /// </summary>
        public static string CurrentCultureName => _currentCulture.Name;

        /// <summary>
        /// Gets the current culture
        /// </summary>
        public static CultureInfo CurrentCulture => _currentCulture;

        /// <summary>
        /// Gets available languages
        /// </summary>
        public static (string Code, string DisplayName)[] AvailableLanguages => new[]
        {
            ("en", GetString("LanguageEnglish")),
            ("zh-CN", GetString("LanguageChinese"))
        };
    }
}
