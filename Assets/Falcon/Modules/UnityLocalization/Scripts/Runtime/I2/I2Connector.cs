/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-23
 */

using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using I2.Loc;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Falcon.Modules.UnityLocalization.Runtime
{
    public static class I2Connector
    {
        [RuntimeInitializeOnLoadMethod]
        private static async void Initialize()
        {
            try
            {
                await Task.Yield();
                await LocalizationSettings.InitializationOperation.Task;
                LocalizationManager.OnLocalizeEvent += OnI2LanguageChanged;
                ULocManager.OnChangeLocale += OnUnityLocaleChanged;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"I2Connector initialization failed: {e.Message}");
            }
        }

        private static void OnI2LanguageChanged()
        {
            try
            {
                var currentLanguage = LocalizationManager.CurrentLanguage;
                foreach (var locale in ULocManager.GetAllLocales())
                {
                    if (LocalizationManager.GetSupportedLanguage(locale.LocaleName) == currentLanguage) 
                    {
                        ULocManager.CurrentLanguage = locale;
                        return;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating Unity Localization from I2: {e.Message}");
            }
        }

        // Callback khi Unity Localization đổi ngôn ngữ -> cập nhật I2 Localization
        private static void OnUnityLocaleChanged(Locale locale)
        {
            if (!Application.isPlaying) return;
            try
            {
                LocalizationManager.CurrentLanguage = locale.LocaleName;
                GameEvent.Emit(ON_LOCALE_CHANGED);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating I2 Localization from Unity: {e.Message}");
            }
        }
        
        private const string ON_LOCALE_CHANGED = "falcon.modules.uloc.on_locale_changed";
    }
}