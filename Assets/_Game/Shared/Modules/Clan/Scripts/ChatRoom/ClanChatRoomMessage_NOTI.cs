using Falcon.Modules.ChatRoom.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared.Clan
{
    public class ClanChatRoomMessage_NOTI : ChatRoomMessageUIBase
    {
        [SerializeField] private TextMeshProUGUI _messageTxt;

        public override void Init(ChatRoomMessageData data)
        {
            Data = data;
            if (_messageTxt != null)
                _messageTxt.text = data?.message_content ?? string.Empty;

            ResetHeight();
        }

        public override void UpdateMessage(ChatRoomMessageData newData) => Init(newData);

        public override void InteractMessage(string interact_content) { }

        private void ResetHeight()
        {
            var rect = GetComponent<RectTransform>();
            if (rect == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            var size = rect.rect.size;
            float y = size.y;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y);
        }
    }
}
