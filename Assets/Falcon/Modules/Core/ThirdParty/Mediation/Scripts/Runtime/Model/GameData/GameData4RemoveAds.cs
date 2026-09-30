/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-04
 */

using Falcon.Modules.Core.AccountData;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FGameDataType("mediation_remove_ads_data")]
    public class GameData4RemoveAds : FGameData<GameData4RemoveAds>
    {
        public bool removeAds;
    }
}