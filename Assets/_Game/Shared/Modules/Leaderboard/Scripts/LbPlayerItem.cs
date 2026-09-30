// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-08-13

using TMPro;
using UnityEngine;

namespace Game.Shared.Leaderboard
{
    /// <summary>Dòng xếp hạng người chơi: thêm level và avatar. Click mở popup profile do UIProfile lo.</summary>
    public class LbPlayerItem : ALbRowItem
    {
        [SerializeField] private TextMeshProUGUI _level;

        [Tooltip("Avatar + frame, kiêm luôn nút mở popup profile")]
        [SerializeField] private GameObject _profile;

        public override System.Type EntryType => typeof(LbPlayerEntry);

        protected override void BindExtra(LbEntry entry)
        {
            if (entry is not LbPlayerEntry e) return;
            if (_level != null) _level.SetText(e.level.ToString());
            if (_profile != null) _profile.SendMessage("UpdateUIOther", (e.avatarId, e.frameId, e.code));
        }
    }
}
