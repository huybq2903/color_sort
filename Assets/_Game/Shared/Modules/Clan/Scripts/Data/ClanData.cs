using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Clan
{
    public class ClanData
    {
        public int code;
        public int owner_code;
        public int avatar_id;
        public string name;
        public string description;
        public int num_member;
        public int max_member;
        public int score;
        public int required_level;
        public List<ClanMemberInfo> member_infos;
        public bool open;
        public bool requested;
        public string bonusData;
        // Không nằm trong payload clan_info gốc, được copy từ SCClanInfo.editable sau khi nhận bản tin
        public bool editable;
    }

    public enum ClanOpenType
    {
        Public = 0,
        Private = 1,
    }
}
