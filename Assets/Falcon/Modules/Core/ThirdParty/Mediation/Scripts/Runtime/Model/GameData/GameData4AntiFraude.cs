/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-14
 */

using Falcon.Modules.Core.AccountData;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FGameDataType("mediation_anti_fraude_data")]
    public class GameData4AntiFraude : FGameData<GameData4AntiFraude>
    {
        public bool sendDataToAppsflyer;
    }
}