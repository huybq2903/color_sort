using Falcon.Modules.ChatRoom.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DemoChatBox : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private ChatBoxBase _chatBoxBase;

    void Start()
    {
        ChatRoomManager.Unregister();
        ChatRoomManager.Register(new DemoChatRoomCustom());
        _inputField.onSubmit.AddListener(OnSendChat);
        new CSJoinChatRoom(_chatBoxBase.room_type, _chatBoxBase.room_id).Send();
    }
    private void OnDestroy()
    {
        new CSOutChatRoom(_chatBoxBase.room_type, _chatBoxBase.room_id).Send();
    }

    private bool _canChat = true;
    private void OnSendChat(string input)
    {
        if (_canChat == false)
            return;
        _canChat = false;

        ChatRoomManager.CreateMessage(_chatBoxBase.room_type, _chatBoxBase.room_id, "TEXT", input,
            onSuccessAction: () =>
            {
                Debug.Log("Send success!");
            }, onFailAction: (message) =>
            {
                Debug.Log("Send failed!");
            }, onTimeoutAction: () =>
            {
                Debug.Log("Time out!");
            }, onDoneAction: () =>
            {
                _canChat = true;
            }, timeOut: 2);
    }
}