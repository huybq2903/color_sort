using Falcon.Modules.ChatRoom.Runtime;
using Falcon.Modules.Core.AccountData;
using UnityEngine;

public class DemoChatRoomCustom : IChatRoomCustom
{
    public string GetMessagePrefabName(ChatRoomMessageData message)
    {
        if (message.sender_code == AccountManager.Instance.ClientData.accountInfo.code)
            return "YourMessagePrefab";
        else
            return "OtherPeoplesMessagePrefab";
    }
}
