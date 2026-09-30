/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-13
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Shop.Runtime
{
    [FAMessage("sc_shop_config")]
    public class SCShopConfig : SCMessage
    {
        public ShopConfig shopConfig;
        public MiniShopConfig miniShopConfig;
        
        /// <summary>
        /// Khi nhận được data thì lưu config vào ShopManager.
        /// </summary>
        public override void OnData()
        {
            ShopManager.SaveShopConfig(shopConfig);
            ShopManager.SaveMiniShopConfig(miniShopConfig);
        }
    }
}