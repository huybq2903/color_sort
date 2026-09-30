/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-03
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using Falcon.Shared.EasyPopup;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Falcon.Shared.Common;

namespace Game.Shared.Profile
{
    public class PopupProfile : UIPopupBase<PopupProfile>, IPopupBackOnFade
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private GameObject goLoading, goContent;

        [Title("Info")]
        [SerializeField] private UIProfileElement avatar, frame;
        [SerializeField] private TMP_Text textDate, textLevel, textName;
        [SerializeField] private Image imageCountry;
        [SerializeField] private UITeam uiTeam;

        [Title("Edit")]
        [SerializeField] private Button btnEdit;

        private int _code = -1;

        private void Awake()
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(OnClickBack);

            btnEdit.onClick.RemoveAllListeners();
            btnEdit.onClick.AddListener(() =>
            {
                PopupEditProfile.Show();
            });
        }

        protected override void OnUIPopupEnable()
        {
            base.OnUIPopupEnable();
            GameEvent.Register(ProfileManager.EVENT_SAVED, OnEditProfile, this);
            goLoading.SetActive(true);
            goContent.SetActive(false);
            if (_code == AccountManager.Instance.Code)
                GetMyInfo();
            else
                GetOthersInfo();
        }

        private void OnEditProfile()
        {
            if (_code == AccountManager.Instance.Code)
            {
                SetName(FGameDataProfile.Instance.playerName);
                SetAvatar(FGameDataProfile.Instance.avatarId);
                SetFrame(FGameDataProfile.Instance.frameId);
            }
        }

        public void SetCode(int code) => _code = code;

        private void GetMyInfo()
        {
            goLoading.SetActive(false);
            goContent.SetActive(true);
            btnEdit.gameObject.SetActive(true);
            textLevel.text = GameRequest<int>.Request(GameKeys.GET_LEVEL).ToString();

            SetName(FGameDataProfile.Instance.playerName);
            SetAvatar(FGameDataProfile.Instance.avatarId);
            SetFrame(FGameDataProfile.Instance.frameId);
            // uiTeam.UpdateUI(_code);

            if (string.IsNullOrEmpty(FGameDataProfile.Instance.countryCode) ||
                string.IsNullOrEmpty(FGameDataProfile.Instance.createDate))
            {
                textDate.text = "Unknown";
                imageCountry.sprite = ProfileManager.Atlas.GetSprite("unknown");
                new CSGetProfile(_code).AddSCListener<SCGetProfile>((message, _, success) =>
                {
                    if (success)
                    {
                        FGameDataProfile.Instance.createDate = message.createDate;
                        FGameDataProfile.Instance.countryCode = message.countryCode;
                        FGameDataProfile.Instance.UpdateToServer();

                        textDate.text = FGameDataProfile.Instance.createDate;
                        imageCountry.sprite = ProfileManager.Atlas.GetSprite(FGameDataProfile.Instance.countryCode);
                    }
                }).Send();
            }
            else
            {
                textDate.text = FGameDataProfile.Instance.createDate;
                imageCountry.sprite = ProfileManager.Atlas.GetSprite(FGameDataProfile.Instance.countryCode);
            }
        }

        private void SetName(string _name) => textName.text = _name;
        private void SetAvatar(int id) => avatar.ActiveItem(id);
        private void SetFrame(int id) => frame.ActiveItem(id);

        private void GetOthersInfo()
        {
            goLoading.SetActive(true);
            goContent.SetActive(false);
            btnEdit.gameObject.SetActive(false);
            // uiTeam.UpdateUI(_code);

            new CSGetProfile(_code).AddSCListener<SCGetProfile>((message, _, success) =>
            {
                if (success)
                {
                    goLoading.SetActive(false);
                    goContent.SetActive(true);
                    textLevel.text = message.level.ToString();

                    SetName(message.playerName);
                    SetAvatar(message.avatarId);
                    SetFrame(message.frameId);
                    textDate.text = message.createDate;
                    imageCountry.sprite = ProfileManager.Atlas.GetSprite($"{message.countryCode}");
                }
            }).Send();
        }

        protected virtual void OnDisable()
        {
            GameEvent.Unregister(ProfileManager.EVENT_SAVED, OnEditProfile, this);
        }
    }
}
