using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("cs_delete_message_chat_room")]
    public class CSDeleteMessage : CSMessageWaitLoginSuccess
    {
        public string room_type;
        public string room_id;
        public string message_uuid;

        public CSDeleteMessage(string room_type, string room_id, string message_uuid)
        {
            this.room_type = room_type;
            this.room_id = room_id;
            this.message_uuid = message_uuid;
        }
    }
}
