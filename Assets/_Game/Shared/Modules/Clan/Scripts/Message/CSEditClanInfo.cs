using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_edit_clan_info")]
    public class CSEditClanInfo : CSMessageWaitLoginSuccess
    {
        public int avatar_id;
        public string name;
        public string description;
        public bool open;
        public int required_level;
        public string bonusData;

        public CSEditClanInfo(int logo, string teamName, string description, bool open, int requiredLevel)
        {
            this.avatar_id = logo;
            this.name = teamName;
            this.description = description;
            this.open = open;
            this.required_level = requiredLevel;
        }
    }
}
