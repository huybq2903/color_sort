// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-08-12

using System.Collections.Generic;
using Falcon.Modules.Core.AccountData;
using Newtonsoft.Json;

namespace Game.Shared.Leaderboard
{
    /// <summary>Phần chung của PlayerLBData và ClanLBData bên server.</summary>
    public abstract class LbEntry
    {
        public int code, rank, score;
        public string name;

        /// <summary>Dòng này có phải của bản thân không; mỗi loại bảng so bằng một thứ khác nhau.</summary>
        [JsonIgnore] public abstract bool IsMe { get; }
    }

    /// <summary>Khớp PlayerLBData: thêm level và profileData.</summary>
    public class LbPlayerEntry : LbEntry
    {
        public int level;
        public string profileData;

        /// <summary>Tách sẵn từ profileData, tránh parse lại mỗi lần scroll rebind row.</summary>
        [JsonIgnore] public int avatarId, frameId;

        public override bool IsMe => code == AccountManager.Instance.Code;

        public void ParseProfile()
        {
            if (string.IsNullOrEmpty(profileData)) return;
            try
            {
                var p = JsonConvert.DeserializeObject<LbProfile>(profileData);
                if (p == null) return;
                avatarId = p.avatarId;
                frameId = p.frameId;
            }
            catch
            {
                // profileData rác thì để avatar/frame mặc định, không làm hỏng cả bảng
            }
        }

        private class LbProfile
        {
            public int avatarId, frameId;
        }
    }

    /// <summary>Khớp ClanLBData: logo clan thay cho avatar, không có level.</summary>
    public class LbClanEntry : LbEntry
    {
        /// <summary>Code clan của mình; module clan báo qua LeaderboardService. 0 = chưa vào clan nào.</summary>
        public static int MyClanCode;

        public int iconId;

        public override bool IsMe => MyClanCode != 0 && code == MyClanCode;
    }

    /// <summary>Kết quả một lần fetch.</summary>
    public class LbPage
    {
        public List<LbEntry> entries = new();
        public LbEntry me;
    }
}
