/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-13
 */

namespace Falcon.Modules.Packs.Core.Runtime
{
    public class PacksConstant
    {
        public const string EVENT_GET_LOCALIZED_PRICE = "falcon.modules.iap.get_localized_price";
        public const string EVENT_PURCHASE = "falcon.modules.iap.purchase";

        public const string EVENT_LOGIN = "falcon.modules.account.login";
        public const string EVENT_CHANGE_DATA = "falcon.modules.packs.on_change_data";
        public const string EVENT_BUY_SUCCESS = "falcon.modules.packs.buy_success";
        public const string EVENT_BUY_SUCCESS_ADD_RESOURCES = "falcon.modules.packs.buy_success.add_resources";
    }
}