using Falcon.Modules.ChatRoom.Runtime;

namespace Game.Shared.Clan
{
    public class ClanChatRoomCustom : IChatRoomCustom
    {
        public string GetMessagePrefabName(ChatRoomMessageData message) => ChatRoomPanel.GetMessagePrefabName(message);
    }
}
