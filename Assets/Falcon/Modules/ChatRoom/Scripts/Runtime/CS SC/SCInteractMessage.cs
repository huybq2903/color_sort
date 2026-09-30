using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("sc_interact_message_chat_room")]
    public class SCInteractMessage : SCMessage
    {
        public string room_type;
        public string room_id;
        public string message_uuid;
        public string message_interact_content;
        public override void OnData()
        {
            ChatRoomEventManager.OnSCInteractMessage(this);
        }
    }
}
