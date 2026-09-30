/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-14


using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.AccountData 
{
    [FAMessage("cs_update_game_data")]
    public class CSUpdateGameData: CSMessageWaitLoginSuccess
    {
        public int sequence;
        public FGameData game_data;
        public CSUpdateGameData(FGameData game_data)
        {
            this.sequence = AccountManager.Instance.Sequence;
            this.game_data = game_data;
        }
    }
}