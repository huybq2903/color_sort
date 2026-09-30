using Falcon.Modules.Core.Network;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("sc_get_messages_chat_room_v2")]
    public class SCGetMessages : SCMessage
    {
        public string room_type;
        public string room_id;
        public List<ChatRoomMessageData> messages;
        public int page;
        public int total_pages;

        public override void OnData()
        {
            ChatRoomEventManager.OnSCGetRoomMessages(this);
        }
    }
}
