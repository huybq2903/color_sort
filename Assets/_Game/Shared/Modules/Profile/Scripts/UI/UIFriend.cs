/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-28
 */

using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.UI.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared.Profile
{
    public class UIFriend : MonoBehaviour
    {
        private const string EVENT_ADD_FRIEND = "falcon.modules.friends.send_friend_request";
        private const string EVENT_REMOVE_FRIEND = "falcon.modules.friends.remove_friend";
        
        public List<Button> btnFriends;

        public void UpdateUI((int id, int status) obj)
        {
            for (int i = 0; i < btnFriends.Count; i++)
            {
                btnFriends[i].gameObject.SetActive(false);
            }

            switch (obj.status)
            {
                case 0:
                case 3:
                    btnFriends[0].gameObject.SetActive(true);
                    btnFriends[0].onClick.RemoveAllListeners();
                    btnFriends[0].onClick.AddListener(() => { GameEvent<int>.Emit(EVENT_ADD_FRIEND, obj.id); });
                    break;
                case 1:
                    btnFriends[1].gameObject.SetActive(true);
                    btnFriends[1].onClick.RemoveAllListeners();
                    btnFriends[1].onClick.AddListener(() =>
                    {
                        GameEvent<int>.Emit(EVENT_REMOVE_FRIEND, obj.id);
                    });
                    break;
                case 2:
                    btnFriends[2].gameObject.SetActive(true);
                    btnFriends[2].onClick.RemoveAllListeners();
                    btnFriends[2].onClick.AddListener(() =>
                    {
                        GameEvent<int>.Emit(EVENT_REMOVE_FRIEND, obj.id);
                    });
                    break;
            }
        }
    }
}