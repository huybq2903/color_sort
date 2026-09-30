using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("cs_get_messages_chatroom_v2")]
    public class CSGetMessages : CSMessageWaitLoginSuccess
    {
        public string room_type;
        public string room_id;
        public int page;

        public CSGetMessages(string room_type, string room_id, int page)
        {
            this.room_type = room_type;
            this.room_id = room_id;
            this.page = page;
        }
    }
}
