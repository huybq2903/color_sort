/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using Falcon.Modules.Core.Network;
using UnityEngine;

namespace Falcon.Modules.Core.AccountData 
{
    public class ConnectionListener: ISessionListener
    {
        public void OnSessionReset()
        {
            AccountManager.Instance.IsLogin = false;
            AccountInfo userInfo = AccountManager.Instance.AccountInfo;
            new CSLogin(userInfo.code, userInfo.token, userInfo.device_id, AccountManager.Instance.Sequence,
                ApkInfo.Instance.app_version_int,
                DeviceInfo.Instance.platform, userInfo.fb_id, userInfo.google_id, userInfo.apple_id).Send();
        }

        public void OnFirstSession()
        {
            AccountInfo userInfo = AccountManager.Instance.AccountInfo;
            new CSLogin(userInfo.code, userInfo.token, userInfo.device_id, AccountManager.Instance.Sequence,
                ApkInfo.Instance.app_version_int,
                DeviceInfo.Instance.platform, userInfo.fb_id, userInfo.google_id, userInfo.apple_id).Send();
        }

        public void OnChannelDisconnected(FChannel fChannel)
        {
            // if (FNetManager.Instance.GetSession().GetChannel() == fChannel)
            // {
            //     AccountManager.Instance.IsLogin = false;
            //     Debug.Log("Current channel disconnected");
            // }
            // else
            // {
            //     Debug.Log("Old channel disconnected");
            // }
        }
    }
}