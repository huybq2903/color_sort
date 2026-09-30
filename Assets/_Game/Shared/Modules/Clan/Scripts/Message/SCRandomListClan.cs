using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("sc_random_list_clan")]
    public class SCRandomListClan : SCMessage
    {
        public ClanDataShort[] clan_infos;
        public string bonusData;

        // Không ai đăng ký nghe broadcast này hiện tại; FetchRandomList() ở ClanService dùng AddSCListener riêng
        public override void OnData() { }
    }
}
