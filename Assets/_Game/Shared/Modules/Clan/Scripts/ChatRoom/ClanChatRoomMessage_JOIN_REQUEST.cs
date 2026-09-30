using Falcon.Modules.ChatRoom.Runtime;
using System;
using TMPro;
using UnityEngine;

using Falcon.Shared.Common;
namespace Game.Shared.Clan
{
    public class ClanChatRoomMessage_JOIN_REQUEST : ChatRoomMessageUIBase
    {
        [SerializeField] ClanChatRoom_PlayerComponentInMessage clanChatRoom_PlayerComponentInMessage;

        private bool _canClick = true;

        public override void Init(ChatRoomMessageData data)
        {
            Data = data;
            clanChatRoom_PlayerComponentInMessage?.InitPlayerData(data.sender_code, data.sender_name, data.profileData, data.bonus_data);
        }

        public override void UpdateMessage(ChatRoomMessageData newData) => Init(newData);

        public override void InteractMessage(string interact_content) { }

        public void AcceptOnClick()
        {
            if (!_canClick)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }
            if (_chatbox == null || Data == null) return;

            _canClick = false;
            ChatRoomManager.InteractMessage(_chatbox.room_type, _chatbox.room_id, Data.message_uuid, "accept",
                onSuccessAction: () => { Debug.Log("Accept Success!"); },
                onFailAction: (message) => { Center.GetOrCreate<ClanService>().ShowToast(message); Debug.Log("Accept Failed: " + message); },
                onTimeoutAction: () => { Center.GetOrCreate<ClanService>().ShowToastTimeout(); Debug.Log("Accept Timeout!"); },
                onDoneAction: () => { _canClick = true; },
                timeOut: 2);
        }

        public void DenyOnClick()
        {
            if (!_canClick)
            {
                Center.GetOrCreate<ClanService>().ShowToastWaitAMomentAfterOnClick();
                return;
            }
            if (_chatbox == null || Data == null) return;

            _canClick = false;
            ChatRoomManager.InteractMessage(_chatbox.room_type, _chatbox.room_id, Data.message_uuid, "refuse",
                onSuccessAction: () => { Debug.Log("Deny Success!"); },
                onFailAction: (message) => { Center.GetOrCreate<ClanService>().ShowToast(message); Debug.Log("Deny Failed: " + message); },
                onTimeoutAction: () => { Center.GetOrCreate<ClanService>().ShowToastTimeout(); Debug.Log("Deny Timeout!"); },
                onDoneAction: () => { _canClick = true; },
                timeOut: 2);
        }
    }
}
