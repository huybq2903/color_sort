using UnityEngine;
using UnityEngine.UI;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Helpers.EventBus;
using Falcon.Shared.Common;

namespace Falcon.InGame.UI
{
    public class UIPopupPause : MonoBehaviour
    {
        private const string EVENT_MEDIA_RSP = "falcon.modules.ui.settings_media_rsp";

        public Button btnExit;
        public Button btnHome;
        public Button btnRestart;

        [Header("Setting - Base")]
        public Button btnSound;
        public GameObject goCrossSound;

        public Button btnVibrate;
        public GameObject goCrossVibrate;

        public Button btnMusic;
        public GameObject goCrossMusic;

        [Header("Cheat - Editor / Development build only")]
        public Button btnWin;
        public Button btnLose;

        protected int _sound;
        protected int _vibrate;
        protected int _music;

        // Gán một lần: onClick không đổi theo ngôn ngữ hay lần mở popup.
        protected virtual void Start()
        {
            btnExit.onClick.AddListener(OnClickExit);
            btnHome.onClick.AddListener(OnClickHome);
            btnSound.onClick.AddListener(OnClickSound);
            btnVibrate.onClick.AddListener(OnClickVibrate);
            btnMusic.onClick.AddListener(OnClickMusic);
            btnRestart.onClick.AddListener(() => GameEvent.Emit(GameKeys.PLAY_LEVEL));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            btnWin.onClick.AddListener(() => OnClickCheat(GameKeys.DO_WIN));
            btnLose.onClick.AddListener(() => OnClickCheat(GameKeys.DO_TEMP_LOSE));
#else
            btnWin.gameObject.SetActive(false);
            btnLose.gameObject.SetActive(false);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnClickCheat(string eventKey)
        {
            UIWrapper.ClosePopup(transform);
            GameEvent.Emit(eventKey);
        }
#endif

        protected virtual void OnEnable()
        {
            GameEvent<(int sound, int vibrate, int music)>.Register(EVENT_MEDIA_RSP, OnMediaSetting, this);
            UpdateUI_Setting();
        }

        protected virtual void OnDisable()
        {
            GameEvent<(int sound, int vibrate, int music)>.Unregister(EVENT_MEDIA_RSP, OnMediaSetting);
        }

        protected virtual void UpdateUI_Setting()
        {
            GameEvent<int>.Emit("falcon.modules.ui.settings_media_req");
        }

        protected virtual void OnClickExit()
        {
            UIWrapper.ClosePopup(transform);
        }

        protected virtual void OnClickHome()
        {
            UIWrapper.ClosePopup(transform);
            UIWrapper.OpenPopup("UIPopupQuitLevel");
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

        private void OnMediaSetting((int sound, int vibrate, int music) data)
        {
            _sound = data.sound;
            _vibrate = data.vibrate;
            _music = data.music;
            ChangeSound();
            ChangeVibrate();
            ChangeMusic();
        }

        private void ChangeSound() => goCrossSound.SetActive(_sound == 0);
        private void ChangeVibrate() => goCrossVibrate.SetActive(_vibrate == 0);
        private void ChangeMusic() => goCrossMusic.SetActive(_music == 0);
        private void SaveMedia(string nameMedia, int value) => GameEvent<(string name, int value)>.Emit(GameKeys.SETTINGS_MEDIA_SAVE, (nameMedia, value));
    }
}
