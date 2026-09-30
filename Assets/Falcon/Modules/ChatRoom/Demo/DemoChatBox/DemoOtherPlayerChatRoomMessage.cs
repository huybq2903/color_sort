using Falcon.Modules.ChatRoom.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class DemoOtherPlayerChatRoomMessage : ChatRoomMessageUIBase
{
    [SerializeField] private Text _nameText, _messageText;

    public override void Init(ChatRoomMessageData data)
    {
        base.Init(data);
        _nameText.text = "User";
        _messageText.text = data.message_content;
    }
}