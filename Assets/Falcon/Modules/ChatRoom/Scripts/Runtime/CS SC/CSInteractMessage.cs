using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("cs_interact_message_chat_room")]
    public class CSInteractMessage : CSMessageWaitLoginSuccess
    {
        public string room_type;
        public string room_id;
        public string message_uuid;
        public string message_interact_content;

        public CSInteractMessage(string room_type, string room_id, string message_uuid, string message_interact_content)
        {
            this.room_type = room_type;
            this.room_id = room_id;
            this.message_uuid = message_uuid;
            this.message_interact_content = message_interact_content;
        }
    }
}
