/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-30
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Packs.Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Packs.PacksRemoveAds.Runtime
{
    public class UIShortcut_RemoveAds : MonoBehaviour
    {
        private const string EVENT_OPEN_POPUP_NAME = "falcon.modules.core.ui_open_popup_name";
        private void Awake()
        {
            var button = GetComponent<Button>();
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                GameEvent<string>.Emit(EVENT_OPEN_POPUP_NAME, "UIPopup_RemoveAds");
            });
        }
    }
}