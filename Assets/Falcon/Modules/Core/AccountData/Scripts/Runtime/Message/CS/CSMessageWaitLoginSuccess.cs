/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.AccountData  
{
    public abstract class CSMessageWaitLoginSuccess : CSMessage
    {
        public override void Send()
        {
            if (AccountManager.Instance.IsLogin)
            {
                base.Send();
            }
            else
            {
                SendMessageWorker.Instance.AddQueue(this);
            }
        }

        public void SendNow()
        {
            base.Send();
        }
    }
}