/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-20
 */

using UnityEngine;

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// ScriptableObject chứa dữ liệu cấu hình mặc định cho shop.
    /// </summary>
    [CreateAssetMenu(fileName = "SOShopConfig", menuName = "Pack/ShopConfig")]
    public class SOShopConfig : ScriptableObject
    {
        public ShopConfig shopConfig;
        public MiniShopConfig miniShopConfig;
    }
}
