using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("sc_new_message_chat_room")]
    public class SCNewMessage : SCMessage
    {
        public ChatRoomMessageData message;
        public override void OnData()
        {
            ChatRoomEventManager.OnSCNewMessage(this);
        }
    }
}
