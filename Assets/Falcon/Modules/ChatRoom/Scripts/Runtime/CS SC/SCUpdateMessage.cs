using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("sc_update_message_chat_room")]
    public class SCUpdateMessage : SCMessage
    {
        public ChatRoomMessageData message;
        public override void OnData()
        {
            ChatRoomEventManager.OnSCUpdateMessage(this);
        }
    }
}
