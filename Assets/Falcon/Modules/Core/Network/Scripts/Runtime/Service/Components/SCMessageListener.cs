// /*
//  * Author: quanph
//  * Email: quanph@falcongames.com
//  * Company: Falcon Games
//  * Date: 2026 - 01 - 09
//  */

namespace Falcon.Modules.Core.Network
{
    public abstract class SCMessageListener<T> : ISCMesageListener<T> where T : SCMessage
    {
        public abstract void OnMessage(T message);

        public void OnMessage(SCMessage message)
        {
            OnMessage((T)message);
        }
    }
}