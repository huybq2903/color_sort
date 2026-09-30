/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.UI.Runtime;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.UI.Settings.Runtime
{
    public class UIPopupSetting : MonoBehaviour
    {
        private const string EVENT_CONFIG_RSP = "falcon.modules.ui.settings_config_rsp";
        private const string EVENT_MEDIA_RSP = "falcon.modules.ui.settings_media_rsp";
        private const string EVENT_ACCOUNT_ID_RSP = "falcon.modules.ui.settings_account_id_rsp";

        public Button btnExit;

        public Button btnSound;
        public GameObject goCrossSound;

        public Button btnVibrate;
        public GameObject goCrossVibrate;

        public Button btnMusic;
        public GameObject goCrossMusic;

        public Button btnTermOfUse;
        public Button btnPrivacyPolicy;
        public Button btnCustomerCare;

        public TextMeshProUGUI txtCode;
        public TextMeshProUGUI txtVersion;

        public Button btnLanguage;
        public TextMeshProUGUI txtLanguage;

        protected int _sound;
        protected int _vibrate;
        protected int _music;

        private string _linkTermOfUse, _linkPrivacyPolicy, _linkCustomerCare;

        // Gán một lần: onClick không đổi theo ngôn ngữ hay lần mở popup.
        protected virtual void Start()
        {
            btnExit.onClick.AddListener(() => UIWrapper.ClosePopup(transform));
            btnSound.onClick.AddListener(OnClickSound);
            btnVibrate.onClick.AddListener(OnClickVibrate);
            btnMusic.onClick.AddListener(OnClickMusic);
            btnLanguage.onClick.AddListener(() => UIWrapper.OpenPopup("UIPopup_Settings_Language"));
            btnTermOfUse.onClick.AddListener(() => OpenLink(_linkTermOfUse));
            btnPrivacyPolicy.onClick.AddListener(() => OpenLink(_linkPrivacyPolicy));
            btnCustomerCare.onClick.AddListener(() => OpenLink(_linkCustomerCare));
        }

        protected virtual void OnEnable()
        {
            LocalizationManager.OnLocalizeEvent += SetTextLocalize;
            GameEvent<List<object>>.Register(EVENT_CONFIG_RSP, OnConfigSetting, this);
            GameEvent<(int sound, int vibrate, int music)>.Register(EVENT_MEDIA_RSP, OnMediaSetting, this);
            GameEvent<int>.Register(EVENT_ACCOUNT_ID_RSP, OnAccountId, this);

            UpdateUI_Setting();
        }

        protected virtual void OnDisable()
        {
            LocalizationManager.OnLocalizeEvent -= SetTextLocalize;
            GameEvent<List<object>>.Unregister(EVENT_CONFIG_RSP, OnConfigSetting);
            GameEvent<(int sound, int vibrate, int music)>.Unregister(EVENT_MEDIA_RSP, OnMediaSetting);
            GameEvent<int>.Unregister(EVENT_ACCOUNT_ID_RSP, OnAccountId);
        }

        protected virtual void UpdateUI_Setting()
        {
            GameEvent<List<string>>.Emit("falcon.modules.ui.settings_config_req", new List<string>() { "linkTermOfUse", "linkPrivacyPolicy", "linkCustomerCare" });
            GameEvent<int>.Emit("falcon.modules.ui.settings_media_req");
            GameEvent<int>.Emit("falcon.modules.ui.settings_account_id_req");

            //Code & Version
            txtCode.SetText(string.Empty);
            txtVersion.SetText($"v.{Application.version}");
            SetTextLocalize();
        }

        private void SetTextLocalize()
        {
            txtLanguage.SetText(LocalizationManager.GetLanguageFromCode(LocalizationManager.CurrentLanguageCode));
        }

        protected virtual void OnClickSound()
        {
            _sound = _sound == 0 ? 1 : 0;
            ChangeSound();
            SaveMedia("sound", _sound);
        }

        protected virtual void OnClickVibrate()
        {
            _vibrate = _vibrate == 0 ? 1 : 0;
            ChangeVibrate();
            SaveMedia("vibrate", _vibrate);
        }

        protected virtual void OnClickMusic()
        {
            _music = _music == 0 ? 1 : 0;
            ChangeMusic();
            SaveMedia("music", _music);
        }

        // Link về async nên có thể bấm trước khi có data -> bỏ qua thay vì mở URL rỗng.
        private static void OpenLink(string url)
        {
            if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
        }

        private void OnConfigSetting(List<object> listFields)
        {
            _linkTermOfUse = listFields[0].ToString();
            _linkPrivacyPolicy = listFields[1].ToString();
            _linkCustomerCare = listFields[2].ToString();
        }

        private void OnMediaSetting((int sound, int vibrate, int music) data)
        {
            _sound = data.sound;
            _vibrate = data.vibrate;
            _music = data.music;
            ChangeSound();
            ChangeVibrate();
            ChangeMusic();
        }

        private void OnAccountId(int code) => txtCode.SetText($"ID: {code}");

        private void ChangeSound() => goCrossSound.SetActive(_sound == 0);
        private void ChangeVibrate() => goCrossVibrate.SetActive(_vibrate == 0);
        private void ChangeMusic() => goCrossMusic.SetActive(_music == 0);
        private void SaveMedia(string nameMedia, int value) => GameEvent<(string name, int value)>.Emit("falcon.modules.ui.settings_media_save", (nameMedia, value));
    }
}