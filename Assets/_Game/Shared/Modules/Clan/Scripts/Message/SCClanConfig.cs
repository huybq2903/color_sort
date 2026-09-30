using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using System;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("sc_clan_config")]
    public class SCClanConfig : SCMessage
    {
        public int my_clan_code;
        public int level_unlock;
        public int max_level_limit;
        public int clanCreateFee;
        public bool isShowStatusOnline;
        public override void OnData()
        {
            Center.GetOrCreate<ClanService>().SetConfig(my_clan_code, level_unlock, max_level_limit, clanCreateFee, isShowStatusOnline);
        }
    }
}
