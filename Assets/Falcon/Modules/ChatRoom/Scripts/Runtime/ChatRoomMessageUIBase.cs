using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    public class ChatRoomMessageUIBase : MonoBehaviour
    {
        private ChatRoomMessageData _data;
        protected ChatBoxBase _chatbox;

        public ChatRoomMessageData Data { get => _data; set => _data = value; }

        internal void InitChatBox(ChatBoxBase chatbox)
        {
            this._chatbox = chatbox;
        }

        public virtual void Init(ChatRoomMessageData data)
        {
            Data = data;
        }

        // Mặc định là gọi lại hàm Init. Nếu muốn diễn anim thay vì init lại, chỉ gán lại data sau đó tự diễn anim update.
        public virtual void UpdateMessage(ChatRoomMessageData newData)
        {
            Init(newData);
        }

        public virtual void InteractMessage(string interact_content)
        {

        }
    }
}
