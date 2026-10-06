/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-18
 */

using Falcon.Modules.Core.InAppPurchase.Runtime;

namespace Falcon.Modules.InAppValidation.CSSC.Runtime
{
    public interface IStoreCSSCValidationHandler
    {
        public void SendValidate(APurchaseProcess purchaseProcess);
        public void Log(APurchaseProcess purchaseProcess);
    }
}