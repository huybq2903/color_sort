/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-18
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.InAppValidation.CSSC.Runtime
{
    [FAMessage("cs_inapp_verify_ios")]
    public class CSInappVerifyIos : CSMessage
    {
        public string productId;
        public string transactionId;
        public string receiptData;

        public CSInappVerifyIos(string productId, string transactionId, string receiptData)
        {
            this.productId = productId;
            this.transactionId = transactionId;
            this.receiptData = receiptData;
        }
    }
}