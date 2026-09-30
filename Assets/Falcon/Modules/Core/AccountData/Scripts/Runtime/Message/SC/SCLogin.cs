/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.AccountData 
{
    [FAMessage("sc_login")]
    public class SCLogin : SCMessage
    {
        public const int LOGIN_STATUS_OK = 0;
        public const int LOGIN_STATUS_ERROR = 1;
        public const int UPDATE_STATUS_NONEEDUPDATE = 0;
        public const int UPDATE_STATUS_UPDATETOSERVER = 1;
        public const int UPDATE_STATUS_UPDATEFROMSERVER = 2;
        public const int UPDATE_STATUS_NEWACCOUNT = 3;

        public int    login_status;
        public int    update_status;
        public string error_message;


        public override void OnData()
        {
            if (login_status == LOGIN_STATUS_ERROR)
            {
                LogUtil.Error("Login failed: " + error_message);
                AccountInfo userInfo = AccountManager.Instance.AccountInfo;
                userInfo.code  = -1;
                userInfo.token = string.Empty;
                new CSLogin(userInfo.code, userInfo.token, userInfo.device_id, AccountManager.Instance.Sequence,
                    ApkInfo.Instance.app_version_int,
                    DeviceInfo.Instance.platform, userInfo.fb_id, userInfo.google_id, userInfo.apple_id).Send();
                AccountManager.Instance.OnLogin(false);
            }
            else if (login_status == LOGIN_STATUS_OK)
            {

                if (update_status == UPDATE_STATUS_NONEEDUPDATE)
                {
                    AccountManager.Instance.IsLogin = true;
                    AccountManager.Instance.OnLogin(true);
                }else if (update_status == UPDATE_STATUS_UPDATETOSERVER)
                {
                    new CSUpdateData(AccountManager.Instance.ClientData).Send();
                    AccountManager.Instance.IsLogin = true;
                    AccountManager.Instance.OnLogin(true);
                }else if (update_status == UPDATE_STATUS_UPDATEFROMSERVER)
                {
                    SendMessageWorker.Instance.RemoveUpdateGameDataMessages();
                }
            }
        }
    }
}