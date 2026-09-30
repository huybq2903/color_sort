using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("sc_help_config_in_clan")]
    public class SCGetHelpConfig : SCMessage
    {
        public long cooldownPerRequest;
        public int coinReceivePerHelp;
        public long nextRequestTimeLeft;
        public int totalHelpResource;
        public override void OnData()
        {
            var service = Center.GetOrCreate<ClanService>();
            service.SetHelpConfig(cooldownPerRequest, coinReceivePerHelp, nextRequestTimeLeft, totalHelpResource);
            service.RaiseGetHelpConfig(this);
        }
    }
}
