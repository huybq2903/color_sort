/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-25
 */

using Falcon.Modules.Core.InAppPurchase.Runtime;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.InAppValidation.CSSC.Runtime
{
    [FAMessage("cs_get_ltv_iap")]
    public class CSGetLtvIap : CSMessage
    {
        
    }
    
    [FAMessage("sc_get_ltv_iap")]
    public class SCGetLtvIap : SCMessage
    {
        public double ltvIap;


        public override void OnData()
        {
            IAPManager.Ltv = ltvIap;
        }
    }
}