using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("sc_delete_message_chat_room")]
    public class SCDeleteMessage : SCMessage
    {
        public string room_type;
        public string room_id;
        public string message_uuid;
        public override void OnData()
        {
            ChatRoomEventManager.OnSCDeleteMessage(this);
        }
    }
}
