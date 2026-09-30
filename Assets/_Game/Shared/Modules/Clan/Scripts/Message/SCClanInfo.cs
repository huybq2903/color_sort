using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("sc_clan_info_v2")]
    public class SCClanInfo : SCMessage
    {
        public bool my_clan;
        public bool editable;
        public ClanData clan_info;
        public string bonusData;
        public override void OnData()
        {
            Center.GetOrCreate<ClanService>().RaiseClanInfo(this);
        }
    }
}
