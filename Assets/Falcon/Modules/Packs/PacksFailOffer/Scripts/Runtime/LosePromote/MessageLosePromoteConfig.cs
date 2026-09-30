/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
 */

using System.Collections.Generic;
using Falcon.Modules.Core.Network;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Packs.PacksFailOffer.Runtime
{
    [FAMessage("cs_get_lose_promote_config")]
    public class CSGetLosePromoteConfig : CSMessage
    {
        
    }
    
    [FAMessage("sc_lose_promote_config")]
    public class SCLosePromoteConfig : SCMessage
    {
        public int quantityShow;
        public List<LosePromoteConfig> configs;
        public override void OnData()
        {
            LosePromoteManager.SaveConfig(this);
        }
    }
}