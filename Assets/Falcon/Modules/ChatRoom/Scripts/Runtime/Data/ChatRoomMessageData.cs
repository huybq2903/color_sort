using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    [System.Serializable]
    public class ChatRoomMessageData
    {
        public int sender_code;
        public string sender_name;
        public string profileData;

        public string message_uuid;
        public string message_type;
        public string message_content;

        public string room_type;
        public string room_id;

        public long created_date;
        public string bonus_data;
    }
}
