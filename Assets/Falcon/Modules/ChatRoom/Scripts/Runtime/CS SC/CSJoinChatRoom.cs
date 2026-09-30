using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [FAMessage("cs_join_chat_room")]
    public class CSJoinChatRoom : CSMessageWaitLoginSuccess
    {
        public string room_type;
        public string room_id;

        public CSJoinChatRoom(string room_type, string room_id)
        {
            this.room_type = room_type;
            this.room_id = room_id;
        }
    }
}
