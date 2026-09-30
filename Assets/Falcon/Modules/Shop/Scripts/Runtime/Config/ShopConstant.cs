/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// Chứa các hằng số và ánh xạ cho hệ thống shop.
    /// </summary>
    public static class ShopConstant
    {
        public const string EVENT_CREATE_PACK_SHOP = "falcon.modules.shop.create_pack_shop";
        public const string EVENT_HIDE_PACK_SHOP = "falcon.modules.shop.hide_pack_shop";
        
        public const string EVENT_CLOSE_POPUP = "falcon.modules.core.ui_close_popup";
        public const string EVENT_LOGIN = "falcon.modules.account.login";
    }
}