using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("cs_create_message_chat_room")]
    public class CSCreateMessage : CSMessageWaitLoginSuccess
    {
        public string room_type;
        public string room_id;
        public string message_type;
        public string message_content;

        public CSCreateMessage(string room_type, string room_id, string message_type, string message_content)
        {
            this.room_type = room_type;
            this.room_id = room_id;
            this.message_type = message_type;
            this.message_content = message_content;
        }
    }
}
