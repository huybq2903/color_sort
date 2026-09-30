/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-24
*/

using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Helpers.GameDataUtils;
using Falcon.Modules.Core.AccountData;
using UnityEngine;

namespace Falcon.Modules.UI.Settings.Runtime
{
    public static class FGameDataUISettingWrapper
    {
        [RuntimeInitializeOnLoadMethod]
        internal static void Init()
        {
            GameEvent<List<string>>.Register("falcon.modules.ui.settings_config_req", listFields =>
            {
                var configSetting = Resources.Load<UISettingConfig>("SO_UI_SettingConfig");

                List<object> data = new();
                for (int i = 0; i < listFields.Count; i++) data.Add(GameDataUtils.GetFieldValueByName(configSetting, listFields[i]));

                GameEvent<List<object>>.Emit("falcon.modules.ui.settings_config_rsp", data);
            });

            GameEvent<int>.Register("falcon.modules.ui.settings_media_req", status =>
            {
                var sound = FGameDataUISetting.Instance.sound;
                var vibrate = FGameDataUISetting.Instance.vibrate;
                var music = FGameDataUISetting.Instance.music;
                GameEvent<(int sound, int vibrate, int music)>.Emit("falcon.modules.ui.settings_media_rsp", (sound, vibrate, music));
            });

            GameRequest<(int sound, int vibrate, int music)>.Register("falcon.modules.ui.settings_get_media", () =>
            {
                var sound = FGameDataUISetting.Instance.sound;
                var vibrate = FGameDataUISetting.Instance.vibrate;
                var music = FGameDataUISetting.Instance.music;
                return (sound, vibrate, music);
            });

            GameRequest<string>.Register("falcon.modules.ui.settings_get_language_code", () =>
            {
                return FGameDataUISetting.Instance.languageCode;
            });

            GameEvent<(string name, int value)>.Register("falcon.modules.ui.settings_media_save", data =>
            {
                if (data.name == "sound") FGameDataUISetting.Instance.sound = data.value;
                if (data.name == "vibrate") FGameDataUISetting.Instance.vibrate = data.value;
                if (data.name == "music") FGameDataUISetting.Instance.music = data.value;
                FGameDataUISetting.Instance.Save();
            });

            GameEvent<string>.Register("falcon.modules.ui.settings_language_save", languageCode =>
            {
                var currentLanguageCode = FGameDataUISetting.Instance.languageCode;
                if (currentLanguageCode == languageCode) return;
                FGameDataUISetting.Instance.languageCode = languageCode;
                FGameDataUISetting.Instance.Save();
            });

            GameEvent<int>.Register("falcon.modules.ui.settings_account_id_req", status =>
            {
                GameEvent<int>.Emit("falcon.modules.ui.settings_account_id_rsp", AccountManager.Instance.Code);
            });
        }
    }
}
