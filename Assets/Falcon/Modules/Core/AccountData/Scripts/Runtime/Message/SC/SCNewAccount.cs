/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.AccountData 
{
    [FAMessage("sc_new_account")]
    public class SCNewAccount : SCMessage
    {
        public int code;
        public string token;
        public string countryCode;


        public override void OnData()
        {
            AccountManager.Instance.AccountInfo.code = code;
            AccountManager.Instance.AccountInfo.token = token;
            AccountManager.Instance.SaveAccountInfo();
            AccountManager.Instance.UpdateToServer();
            AccountManager.Instance.IsLogin = true;
            AccountManager.Instance.OnLogin(true);
        }
    }
}