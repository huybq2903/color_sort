/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-14

using Falcon.Modules.Core.Network;
using Newtonsoft.Json;

namespace Falcon.Modules.Core.AccountData
{
    [FAMessage("sc_update_game_data")]
    public class SCUpdateGameData : SCMessage
    {
        [JsonConverter(typeof(FGameDataConverter))]
        public FGameData gameData;
        public override void OnData()
        {
            AccountManager.Instance.GameDatas[gameData.__type] = gameData;
            gameData.Save();
            gameData.OnUpdateFromServer();
        }
    }
}