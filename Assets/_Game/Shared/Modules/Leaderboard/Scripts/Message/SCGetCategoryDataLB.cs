using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Game.Shared.Leaderboard
{
    [FAMessage("sc_get_category_data_lb")]
    public class SCGetCategoryDataLB : SCMessage
    {
        public long timeLeft;
        public string bonusData;

        public override void OnData()
        {
            
        }
    }
}
