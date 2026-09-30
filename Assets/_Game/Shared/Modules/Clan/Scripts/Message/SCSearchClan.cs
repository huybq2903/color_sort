using Falcon.Modules.Core.Network;

namespace Game.Shared.Clan
{
    [FAMessage("sc_search_clan")]
    public class SCSearchClan : SCMessage
    {
        public ClanDataShort[] clan_infos;
        public string bonusData;

        public override void OnData()
        {
        }
    }
}
