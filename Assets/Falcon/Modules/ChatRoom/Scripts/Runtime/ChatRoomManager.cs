using Falcon.Modules.Core.Network;
using System;
using UnityEngine;

namespace Falcon.Modules.ChatRoom.Runtime
{
    public class ChatRoomManager
    {
        private static IChatRoomCustom _custom;

        public static void Register(IChatRoomCustom custom)
        {
            if (_custom == null)
                _custom = custom;
        }
        public static void Unregister()
        {
            _custom = null;
        }

        public static string GetMessagePrefabName(ChatRoomMessageData message)
        {
            return (_custom != null ? _custom.GetMessagePrefabName(message) : "");
        }

        public static void CreateMessage(string room_type, string room_id, string message_type, string message_content, Action onSuccessAction = null, Action<string> onFailAction = null, Action onTimeoutAction = null, Action onDoneAction = null, int timeOut = 5)
        {
            new CSCreateMessage(room_type, room_id, message_type, message_content).AddSCListener<SCResponse_Chat>((message, timeout, success) =>
            {
                if (success && message != null)
                {
                    if (message.success)
                    {
                        onSuccessAction?.Invoke();
                    }
                    else
                    {
                        onFailAction?.Invoke(message.message);
                    }
                }
                else if (timeout)
                    onTimeoutAction?.Invoke();
                else
                    onFailAction?.Invoke("");

                onDoneAction?.Invoke();

            }, timeOut).Send();
        }

        public static void EditMessage(string room_type, string room_id, string message_uuid, string message_type, string message_content, Action onSuccessAction = null, Action<string> onFailAction = null, Action onTimeoutAction = null, Action onDoneAction = null, int timeOut = 5)
        {
            new CSEditMessage(room_type, room_id, message_uuid, message_type, message_content).AddSCListener<SCResponse_Chat>((message, timeout, success) =>
            {
                if (success && message != null)
                {
                    if (message.success)
                    {
                        onSuccessAction?.Invoke();
                    }
                    else
                    {
                        onFailAction?.Invoke(message.message);
                    }
                }
                else if (timeout)
                    onTimeoutAction?.Invoke();
                else
                    onFailAction?.Invoke("");

                onDoneAction?.Invoke();

            }, timeOut).Send();
        }

        public static void DeleteMessage(string room_type, string room_id, string message_uuid, Action onSuccessAction = null, Action<string> onFailAction = null, Action onTimeoutAction = null, Action onDoneAction = null, int timeOut = 5)
        {
            new CSDeleteMessage(room_type, room_id, message_uuid).AddSCListener<SCResponse_Chat>((message, timeout, success) =>
            {
                if (success && message != null)
                {
                    if (message.success)
                    {
                        onSuccessAction?.Invoke();
                    }
                    else
                    {
                        onFailAction?.Invoke(message.message);
                    }
                }
                else if (timeout)
                    onTimeoutAction?.Invoke();
                else
                    onFailAction?.Invoke("");

                onDoneAction?.Invoke();

            }, timeOut).Send();
        }

        public static void InteractMessage(string room_type, string room_id, string message_uuid, string message_interact_content, Action onSuccessAction = null, Action<string> onFailAction = null, Action onTimeoutAction = null, Action onDoneAction = null, int timeOut = 5)
        {
            new CSInteractMessage(room_type, room_id, message_uuid, message_interact_content).AddSCListener<SCResponse_Chat>((message, timeout, success) =>
            {
                if (success && message != null)
                {
                    if (message.success)
                    {
                        onSuccessAction?.Invoke();
                    }
                    else
                    {
                        onFailAction?.Invoke(message.message);
                    }
                }
                else if (timeout)
                    onTimeoutAction?.Invoke();
                else
                    onFailAction?.Invoke("");

                onDoneAction?.Invoke();

            }, timeOut).Send();
        }
    }
}
