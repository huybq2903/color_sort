using Falcon.Modules.ChatRoom.Runtime;
using Falcon.Modules.Core.AccountData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class ChatRoomPanel : MonoBehaviour
    {
        // Task 6: chuyển từ interface custom cũ (GetMessagePrefabName), gọi bởi ClanChatRoomCustom
        private enum ClanMessageType { TEXT = 0, NOTI = 1, HELP = 2, JOIN_REQUEST = 3 }

        /// <summary>Trả tên prefab tin nhắn tương ứng với message_type, dùng trong SuperScrollView.</summary>
        internal static string GetMessagePrefabName(ChatRoomMessageData message)
        {
            var messageType = (ClanMessageType)Enum.Parse(typeof(ClanMessageType), message.message_type);
            switch (messageType)
            {
                case ClanMessageType.TEXT:
                    return message.sender_code == AccountManager.Instance.ClientData.accountInfo.code
                        ? "YourTextMessagePrefab" : "OtherTextMessagePrefab";
                case ClanMessageType.NOTI:
                    return "NotiMessagePrefab";
                case ClanMessageType.HELP:
                    return message.sender_code == AccountManager.Instance.ClientData.accountInfo.code
                        ? "YourHelpMessagePrefab" : "OtherHelpMessagePrefab";
                case ClanMessageType.JOIN_REQUEST:
                    return "JoinRequestPrefab";
            }
            return "NotiMessagePrefab";
        }

        [SerializeField] private ClanLogoUI _logo;
        [SerializeField] private TextMeshProUGUI _clanNameTxt;
        [SerializeField] private ChatBoxInClan _clanChatbox;

        private ClanData _clanData = null;
        private bool joinChatRoom = false;

        private void Awake()
        {
            if (_clanNameTxt != null) _clanNameTxt.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void OnDisable()
        {
            // Rời phòng khi panel bị disable
            if (_clanData != null)
            {
                new CSOutChatRoom("CLAN", "CLAN_" + _clanData.code).Send();
            }
            joinChatRoom = false;
        }

        public void InitClanData(ClanData data)
        {
            int beforeClanCode = _clanData != null ? _clanData.code : -1;
            _clanData = data;

            _logo?.Init(data.avatar_id);
            if (_clanNameTxt != null) _clanNameTxt.text = data.name;
            _clanChatbox?.InitChatRoomId("CLAN_" + data.code);

            if (!joinChatRoom)
            {
                joinChatRoom = true;
                new CSJoinChatRoom("CLAN", "CLAN_" + data.code).Send();
            }

            if (beforeClanCode != data.code && _clanChatbox != null && _clanChatbox.gameObject.activeInHierarchy)
            {
                _clanChatbox.GetFirstPage();
            }
        }

        public void InfoButtonOnClick()
        {
            if (_clanData == null) return;
            Center.GetOrCreate<ClanService>().ShowClanInfoPopup(_clanData.code, true);
        }
    }
}
