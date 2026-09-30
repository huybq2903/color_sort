/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-22
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Falcon.Modules.UnityLocalization.Runtime
{
    public static class ULocManager
    {
        private const string KEY_SAVE_LANGUAGE = "ULoc_Language";
        private const string GET_LOCALIZED_STRING = "falcon.modules.uloc.get_localized_string";
        private const string ON_LOCALE_CHANGED = "falcon.modules.uloc.on_locale_changed";
        public static event Action<Locale> OnChangeLocale
        {
            add => LocalizationSettings.SelectedLocaleChanged += value;
            remove => LocalizationSettings.SelectedLocaleChanged -= value;
        }

        [RuntimeInitializeOnLoadMethod]
        private static async void OnInitialize()
        {
            try
            {
                GameRequest<(string, object[]), string>.Register(GET_LOCALIZED_STRING, GetLocalizedString);
                GameRequest<string, string>.Register(GET_LOCALIZED_STRING, GetLocalizedString);
                LocalizationSettings.SelectedLocaleChanged += _ => GameEvent.Emit(ON_LOCALE_CHANGED);
                await LocalizationSettings.InitializationOperation.Task;
                await Task.Yield();
                LocalizationSettings.SelectedLocale = CurrentLanguage;
            }
            catch (Exception e)
            {
                Debug.LogError($"Localization initialization failed: {e.Message}");
            }
        }

        private static string GetLocalizedString(string entryName)
        {
            return GetLocalizedString((entryName, Array.Empty<object>()));
        }

        private static string GetLocalizedString((string, object[]) arg)
        {
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                Debug.LogWarning($"LocalizationSettings not initialized yet. Returning entryKey: {arg.Item1}");
                return arg.Item1;
            }
            return LocalizationSettings.StringDatabase.GetLocalizedString(arg.Item1, arg.Item2);
        }

        public static Locale CurrentLanguage
        {
            set
            {
                if (LocalizationSettings.SelectedLocale == value) return;
                LocalizationSettings.SelectedLocale = value;
                SaveLoadHandler.Save(KEY_SAVE_LANGUAGE, value.Identifier.Code);
            }
            get
            {
                if (!LocalizationSettings.InitializationOperation.IsDone)
                {
                    Debug.LogWarning("LocalizationSettings not initialized yet. Returning SelectedLocale.");
                    return LocalizationSettings.SelectedLocale;
                }
                
                var locales = LocalizationSettings.AvailableLocales.Locales;
                var savedCode = SaveLoadHandler.Load(KEY_SAVE_LANGUAGE, string.Empty);
                if (!string.IsNullOrEmpty(savedCode))
                {
                    var savedLocale = locales.FirstOrDefault(l =>
                        string.Equals(l.Identifier.Code, savedCode, StringComparison.OrdinalIgnoreCase));
                    if (savedLocale != null) return savedLocale;
                }

                var deviceLocale = GetDeviceLocale(locales);
                return deviceLocale ?? LocalizationSettings.SelectedLocale;
            }
        }

        /// <summary>
        /// Lấy ra string từ một entry key trong bảng String Table hiện tại.
        /// </summary>
        /// <param name="entryKey">Key của entry</param>
        /// <param name="arguments">các tham số đi cùng nếu có</param>
        /// <returns>String localized</returns>
        public static string GetLocalizedString(string entryKey, params object[] arguments)
        {
            return GetLocalizedString((entryKey, arguments));
        }
        
        /// <summary>
        /// Lấy ra string từ một entry key trong bảng String Table cụ thể
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entryKey"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static string GetLocalizedStringFromTable(string table, string entryKey, params object[] arguments)
        {
            // Check if LocalizationSettings is initialized
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                Debug.LogWarning($"LocalizationSettings not initialized yet. Returning entryKey: {entryKey}");
                return entryKey;
            }
            
            return LocalizationSettings.StringDatabase.GetLocalizedString(table, entryKey, new List<object>() {"5"});
        }
        
        /// <summary>
        /// Lấy danh sách tất cả các ngôn ngữ có trong project.
        /// </summary>
        public static List<Locale> GetAllLocales()
        {
            // Check if LocalizationSettings is initialized
            if (!LocalizationSettings.InitializationOperation.IsDone)
            {
                Debug.LogWarning("LocalizationSettings not initialized yet. Returning empty list.");
                return new List<Locale>();
            }
            
            return LocalizationSettings.AvailableLocales.Locales;
        }

        private static Locale GetDeviceLocale(IReadOnlyList<Locale> locales)
        {
            if (locales == null || locales.Count == 0) return null;

            var culture = CultureInfo.CurrentCulture;
            var systemLanguage = Application.systemLanguage.ToString();
            var candidates = new[]
            {
                culture.Name,
                culture.TwoLetterISOLanguageName,
                systemLanguage
            }.Where(s => !string.IsNullOrEmpty(s)).ToArray();

            foreach (var locale in locales)
            {
                var identifier = locale.Identifier;
                var localeCulture = identifier.CultureInfo;

                bool Match(string value, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
                {
                    if (string.IsNullOrEmpty(value)) return false;
                    return candidates.Any(c => string.Equals(c, value, comparison));
                }

                if (Match(identifier.Code)) return locale;
                if (localeCulture != null &&
                    (Match(localeCulture.Name) ||
                     Match(localeCulture.TwoLetterISOLanguageName) ||
                     Match(localeCulture.EnglishName)))
                {
                    return locale;
                }

                if (Match(locale.LocaleName)) return locale;
            }

            return null;
        }
    }
}