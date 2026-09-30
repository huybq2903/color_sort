using UnityEngine;

namespace Game.Shared.Clan
{
    public class ClanMemberInfo
    {
        public int code;
        public string name;
        public int role;
        public int level;
        public int contribution;
        public bool canRemove;
        public bool canSetRole;
        public string profileData;
        public string bonusData;
    }

    public enum ClanRole
    {
        owner = 0,
        co_owner = 1,
        member = 2,
    }
}
