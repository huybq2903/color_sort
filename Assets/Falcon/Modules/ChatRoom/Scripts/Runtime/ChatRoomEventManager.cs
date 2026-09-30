using UnityEngine;
using UnityEngine.Events;

namespace Falcon.Modules.ChatRoom.Runtime
{
    public class ChatRoomEventManager
    {
        public static UnityAction<SCGetMessages> onSCGetRoomMessagesAction;
        public static UnityAction<SCNewMessage> onSCNewMessageAction;
        public static UnityAction<SCUpdateMessage> onSCUpdateMessageAction;
        public static UnityAction<SCDeleteMessage> onSCDeleteMessageAction;
        public static UnityAction<SCInteractMessage> onSCInteractMessageAction;

        public static void OnSCGetRoomMessages(SCGetMessages scGetRoomMessages)
        {
            onSCGetRoomMessagesAction?.Invoke(scGetRoomMessages);
        }
        public static void OnSCNewMessage(SCNewMessage sCNewMessage)
        {
            onSCNewMessageAction?.Invoke(sCNewMessage);
        }
        public static void OnSCUpdateMessage(SCUpdateMessage sCUpdateMessage)
        {
            onSCUpdateMessageAction?.Invoke(sCUpdateMessage);
        }
        public static void OnSCDeleteMessage(SCDeleteMessage sCDeleteMessage)
        {
            onSCDeleteMessageAction?.Invoke(sCDeleteMessage);
        }
        public static void OnSCInteractMessage(SCInteractMessage sCInteractMessage)
        {
            onSCInteractMessageAction?.Invoke(sCInteractMessage);
        }
    }
}
