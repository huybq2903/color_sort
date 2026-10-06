/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-18
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.InAppValidation.CSSC.Runtime
{
    [FAMessage("cs_inapp_info")]
    public class CSInappInfo : CSMessage
    {
        public string product_id;
        public string placement;
        public double localized_price;
        public string iso_currency_code;
        public string transaction_id;
        public string purchase_token;
    }
}