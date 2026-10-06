/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-18
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.InAppValidation.CSSC.Runtime
{
    [FAMessage("cs_inapp_verify_android")]
    public class CSInappVerifyAndroid : CSMessage
    {
        public string productId;
        public string purchaseToken;
        public string packageName;

        public CSInappVerifyAndroid(string productId, string purchaseToken, string packageName)
        {
            this.productId = productId;
            this.purchaseToken = purchaseToken;
            this.packageName = packageName;
        }
    }
}