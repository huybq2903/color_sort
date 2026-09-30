// /*
//  * Author: quanph
//  * Email: quanph@falcongames.com
//  * Company: Falcon Games
//  * Date: 2026 - 01 - 09
//  */

using Falcon.Helpers.FReflection;

namespace Falcon.Modules.Core.Network
{
    internal interface ISCMessageListener : IFReflection
    {
        public void OnMessage(SCMessage message);
    }
}