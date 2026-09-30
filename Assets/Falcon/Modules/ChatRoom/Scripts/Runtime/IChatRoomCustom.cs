using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    public interface IChatRoomCustom
    {
        /// <summary>
        /// Trả về tên của message prefab (nằm trong Scrollview của bạn) tùy theo ChatRoomMessageData.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        string GetMessagePrefabName(ChatRoomMessageData message);
    }
}
