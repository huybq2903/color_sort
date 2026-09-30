/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using Falcon.Helpers.EventBus;
using Falcon.Shared.EasyPopup;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Falcon.Shared.Common;

namespace Game.Shared.Profile
{
    public class PopupEditProfileName : UIPopupBase<PopupEditProfileName>
    {
        [SerializeField] private TMP_InputField inputFieldName;
        [SerializeField] private Button btnClose, btnSave;

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickBack);
            btnSave.onClick.AddListener(OnSave);
            btnClose.gameObject.SetActive(false);
        }

        private void OnSave()
        {
            if (!inputFieldName.text.IsValidName())
            {
                GameEvent<string>.Emit(GameKeys.TOAST_OPEN, "toast_name_is_not_valid");
            }
            else if (inputFieldName.text.Equals(FGameDataProfile.Instance.playerName))
            {
                GameEvent<string>.Emit(GameKeys.TOAST_OPEN, "not_use_default_name");
            }
            else
            {
                FGameDataProfile.Instance.playerName = inputFieldName.text;
                FGameDataProfile.Instance.UpdateToServer();

                GameEvent.Emit(ProfileManager.EVENT_SAVED);
                OnClickBack();
            }
        }

        public void UpdateUI()
        {
            inputFieldName.text = FGameDataProfile.Instance.playerName;
        }
    }
}