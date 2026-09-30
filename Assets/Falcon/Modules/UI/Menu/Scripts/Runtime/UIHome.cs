/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using System;
using System.Collections;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class UIHome : MonoBehaviour
    {
        public Button btnProfile;
        public RectTransform avatar;
        public RectTransform frame;
        public Button btnSettings;

        [Header("Shortcut")]
        public UIHomeShortcuts shortcut;
        public UIHomeShortcutsReceiveBus shortcutReceiveBus;

        protected virtual void Start()
        {
            StartCoroutine(IEConfirmLoadUIComplete());
            IEnumerator IEConfirmLoadUIComplete()
            {
                yield return new WaitForEndOfFrame();
                GameEvent<int>.Emit(MenuConst.EVENT_UI_HOME_LOAD_COMPLETE);
            }

            btnSettings.onClick.RemoveAllListeners();
            btnSettings.onClick.AddListener(() =>
            {
                GameEvent<int>.Emit("falcon.modules.ui.settings_open");
            });
        }

        protected virtual void OnEnable()
        {
            GameEvent.Register("falcon.modules.ui.edit_profile_click_save", UpdateUI, this);
            UpdateUI();
        }

        protected virtual void OnDisable()
        {
            GameEvent.Unregister("falcon.modules.ui.edit_profile_click_save", UpdateUI, this);
        }

        protected virtual void UpdateUI()
        {
            btnProfile.onClick.RemoveAllListeners();
            btnProfile.onClick.AddListener(() =>
            {
                GameEvent.Emit("falcon.modules.ui.profile_open_mine");
            });

            GameEvent<Action<int>>.Emit("falcon.modules.ui.profile_get_avatar_id_req", id =>
            {
                avatar.SendMessage("ActiveItem", id, SendMessageOptions.DontRequireReceiver);

                // Load facebook avatar if available
                var url = AccountManager.Instance.ClientData.accountInfo.avatar_url;
                for (int i = 0; i < avatar.childCount; i++)
                {
                    avatar.GetChild(i).gameObject.SendMessage("Load", url, SendMessageOptions.DontRequireReceiver);
                }
            });

            GameEvent<Action<int>>.Emit("falcon.modules.ui.profile_get_frame_id_req", id => frame.SendMessage("ActiveItem", id, SendMessageOptions.DontRequireReceiver));
        }
    }
}
