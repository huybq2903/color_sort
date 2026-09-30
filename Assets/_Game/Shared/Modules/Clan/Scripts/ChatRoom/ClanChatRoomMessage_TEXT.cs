using Falcon.Modules.ChatRoom.Runtime;
using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared.Clan
{
    public class ClanChatRoomMessage_TEXT : ChatRoomMessageUIBase
    {
        [SerializeField] ClanChatRoom_PlayerComponentInMessage clanChatRoom_PlayerComponentInMessage;
        [SerializeField] TextMeshProUGUI _messageTxt;
        [SerializeField] TextMeshProUGUI _timeTxt;

        private static readonly CultureInfo cultureInfo = CultureInfo.InvariantCulture;

        public override void Init(ChatRoomMessageData data)
        {
            Data = data;
            clanChatRoom_PlayerComponentInMessage?.InitPlayerData(data.sender_code, data.sender_name, data.profileData, data.bonus_data);
            if (_messageTxt != null) _messageTxt.text = data?.message_content ?? string.Empty;
            if (_timeTxt != null) _timeTxt.text = FormatMessageTime(data?.created_date ?? 0);
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
            float y = size.y < 230f ? 230f : size.y;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y);
        }

        // createdDate: Unix time (giay/milli); cung ngay -> HH:mm, cung nam -> dd/MM, khac nam -> dd/MM/yyyy
        private string FormatMessageTime(long createdDate)
        {
            long ms = createdDate < 1_000_000_000_000L ? createdDate * 1000L : createdDate;

            DateTime dt = DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime().DateTime;
            DateTime now = DateTime.Now;

            if (dt.Date == now.Date)
                return dt.ToString("HH:mm", cultureInfo);

            if (dt.Year == now.Year)
                return dt.ToString("dd/MM", cultureInfo);

            return dt.ToString("dd/MM/yyyy", cultureInfo);
        }
    }
}
