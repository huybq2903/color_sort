/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-07


using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.AccountData
{
    [FAMessage("sc_update_data")]
    public class SCUpdateData : SCMessage
    {
        public ClientData clientData;
        public override void OnData()
        {
            AccountManager.Instance.UpdateDataFromServer(clientData);
            AccountManager.Instance.IsLogin = true;
            AccountManager.Instance.OnLogin(true);
        }
    }
}