/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-30
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared.Profile
{
    public class UIProfile : MonoBehaviour
    {
        [SerializeField] private UIProfileElement avatar, frame;
        [SerializeField] private bool isMe = true;
        private int _code;
        private void Awake()
        {
            var btnProfile = GetComponent<Button>();
            btnProfile.onClick.AddListener(() =>
            {
                GameEvent<int>.Emit("game.profile.open", _code);
            });
        }

        private void OnEnable()
        {
            if (isMe)
            {
                _code = AccountManager.Instance.Code;
                GameEvent.Register(ProfileManager.EVENT_SAVED, UpdateUI, this);
                UpdateUI();
            }
        }

        private void OnDisable()
        {
            if (isMe)
            {
                GameEvent.Unregister(ProfileManager.EVENT_SAVED, UpdateUI, this);
            }
        }

        private void UpdateUI()
        {
            avatar.ActiveItem(FGameDataProfile.Instance.avatarId);
            frame.ActiveItem(FGameDataProfile.Instance.frameId);
        }

        public void UpdateUIOther((int ava, int frame, int code) data)
        {
            avatar.ActiveItem(data.ava);
            frame.ActiveItem(data.frame);
            _code = data.code;
        }

        public void UpdateUI(int avatarId, int frameId)
        {
            avatar.ActiveItem(avatarId);
            frame.ActiveItem(frameId);
            _code = -1;
        }
    }
}