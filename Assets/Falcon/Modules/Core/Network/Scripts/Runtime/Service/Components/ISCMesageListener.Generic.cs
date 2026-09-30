// /*
//  * Author: quanph
//  * Email: quanph@falcongames.com
//  * Company: Falcon Games
//  * Date: 2026 - 01 - 09
//  */

namespace Falcon.Modules.Core.Network
{
    internal interface ISCMesageListener<T> : ISCMessageListener where T : SCMessage
    {
        public void OnMessage(T message);
    }
}