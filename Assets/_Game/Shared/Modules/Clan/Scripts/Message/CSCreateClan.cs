using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_create_clan")]
    public class CSCreateClan : CSMessageWaitLoginSuccess
    {
        public int avatar_id;
        public string name;
        public string description;
        public bool open;
        public int required_level;
        public string bonusData;

        public CSCreateClan(int avatar_id, string name, string description, bool open, int required_level)
        {
            this.avatar_id = avatar_id;
            this.name = name;
            this.description = description;
            this.open = open;
            this.required_level = required_level;
        }
    }
}
