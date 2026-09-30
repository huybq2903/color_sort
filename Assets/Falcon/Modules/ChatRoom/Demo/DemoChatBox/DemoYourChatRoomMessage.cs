using Falcon.Modules.ChatRoom.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class DemoYourChatRoomMessage : ChatRoomMessageUIBase
{
    [SerializeField] private Text _messageText;

    public override void Init(ChatRoomMessageData data)
    {
        base.Init(data);
        _messageText.text = data.message_content;
    }
}