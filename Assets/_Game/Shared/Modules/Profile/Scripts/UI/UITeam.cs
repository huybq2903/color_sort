/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using System;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared.Profile
{
    public class UITeam : MonoBehaviour
    {
        public Image icon;
        public Text txtName;
        public Button clanBtn;
        public Sprite sprNoClan;

        public void UpdateUI(int code)
        {
            GameEvent<(int, Action<int, string, Sprite>)>.Emit("falcon.modules.clan.get_user_clan", (code, Callback));
        }

        private void Callback(int clanId, string clanName, Sprite clanIcon)
        {
            if (clanId != 0 && !string.IsNullOrEmpty(clanName))
            {
                txtName.text = clanName;
                icon.sprite = clanIcon;
                clanBtn.onClick.RemoveAllListeners();
                clanBtn.interactable = true;
                clanBtn.onClick.AddListener(() =>
                {
                    GameEvent<int>.Emit("falcon.modules.clan.open_clan_info_popup", clanId);
                });
            }
            else
            {
                icon.sprite = sprNoClan;
                clanBtn.interactable = false;
                txtName.text = "no_team";
            }
        }
    }
}