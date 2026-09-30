// /*
//  * Author: quanph
//  * Email: quanph@falcongames.com
//  * Company: Falcon Games
//  * Date: 2026 - 01 - 09
//  */

using Falcon.Modules.Core.AccountData;
using UnityEngine;

namespace Falcon.Modules.Core.Network.Test
{
    public class SCLoginListener : SCMessageListener<SCLogin>
    {
        public override void OnMessage(SCLogin message)
        {
            Debug.Log("SCLoginListener");
        }
    }
}