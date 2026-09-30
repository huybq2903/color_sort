// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-08-13

using System;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared.Leaderboard
{
    /// <summary>Dòng xếp hạng clan: logo do module clan cấp, click mở popup thông tin clan.</summary>
    public class LbClanItem : ALbRowItem
    {
        // Cùng key với module clan cũ (đã đăng ký sẵn)
        private const string EVENT_GET_LOGO = "falcon.modules.clan.get_logo_trans";
        private const string EVENT_OPEN_INFO = "falcon.modules.clan.open_clan_info_popup";

        [Tooltip("Chỗ gắn logo clan lấy từ module clan")]
        [SerializeField] private RectTransform _logoAnchor;

        private int _code;

        private void Awake()
        {
            var btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OpenClanInfo);
        }

        private void OpenClanInfo() => GameEvent<int>.Emit(EVENT_OPEN_INFO, _code);

        public override Type EntryType => typeof(LbClanEntry);

        protected override void BindExtra(LbEntry entry)
        {
            if (entry is not LbClanEntry e) return;
            _code = e.code;
            if (_logoAnchor == null) return;

            RectTransform logo = null;
            GameEvent<(int, Action<RectTransform>)>.Emit(EVENT_GET_LOGO, (e.iconId, t => logo = t));
            if (logo == null) return; // chua mo clan thi khong ai tra loi, bo qua

            logo.SetParent(_logoAnchor, false);
            logo.anchorMin = Vector2.zero;
            logo.anchorMax = Vector2.one;
            logo.offsetMin = Vector2.zero;
            logo.offsetMax = Vector2.zero;
        }
    }
}
