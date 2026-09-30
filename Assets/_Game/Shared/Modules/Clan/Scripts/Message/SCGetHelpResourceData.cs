using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using System.Collections.Generic;

namespace Game.Shared.Clan
{
    [FAMessage("sc_help_resource_data_in_clan")]
    public class SCGetHelpResourceData : SCMessage
    {
        public List<HelpResourceRowData> data;

        public override void OnData()
        {
            var service = Center.GetOrCreate<ClanService>();
            service.TotalHelpResource = data.Count;
            service.RaiseGetHelpResourceData(this);
        }
    }

    [System.Serializable]
    public class HelpResourceRowData
    {
        public int player_code;
        public string player_name;
    }
}
