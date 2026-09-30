/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
*/

using Falcon.Helpers.EventBus;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.UI.Settings.Runtime
{
    public class UIPopupSettingLanguageItem : MonoBehaviour
    {
        public TextMeshProUGUI txtCode;
        public Button btnSelect;
        public RectTransform rectSelected;

        public virtual void SetItemData(string code)
        {
            txtCode.SetText(LocalizationManager.GetLanguageFromCode(code));
            btnSelect.onClick.RemoveAllListeners();
            btnSelect.onClick.AddListener(() =>
            {
                LocalizationManager.CurrentLanguageCode = code;
                GameEvent<string>.Emit("falcon.modules.ui.settings_language_save", code);
            });
            rectSelected.gameObject.SetActive(LocalizationManager.CurrentLanguageCode == code);
        }
    }
}
