/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-24
 */

using Falcon.Modules.Core.AccountData;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Runtime
{
    [FGameDataType("adjust_data")]
    public class GameData4Adjust : FGameData<GameData4Adjust>
    {
        public string adjustID;
        public string trackerToken;
        public string network;
        public string campaign;
        public string adGroup;
        public string creative;
        public string costType;
        public string costAmount;
        public string costCurrency;
    }
}