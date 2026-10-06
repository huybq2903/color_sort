/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-17
 */

using Falcon.Modules.Core.InAppPurchase.Runtime;

namespace Falcon.Modules.InAppValidation.CSSC.Runtime
{
    public class FakeStoreCSSCValidationHandler : IStoreCSSCValidationHandler
    {
        public void SendValidate(APurchaseProcess purchaseProcess)
        {
            purchaseProcess.OnReceiveValidation(APurchaseProcess.State.Purchased);
        }

        public void Log(APurchaseProcess purchaseProcess)
        {
            
        }
    }
}