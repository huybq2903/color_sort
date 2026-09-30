/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-13
 */

using System;

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// Cấu hình cho UI mini shop.
    /// </summary>
    [Serializable]
    public class MiniShopConfig
    {
        public MiniShopElementConfig[] elementConfigs;
    }
    
    /// <summary>
    /// Thông tin từng pack trong mini shop.
    /// </summary>
    [Serializable]
    public class MiniShopElementConfig
    {
        /// <summary>ID duy nhất cho pack.</summary>
        public string idPack;
        /// <summary>Vị trí hiển thị (1–4).</summary>
        public int position;
        /// <summary>Độ ưu tiên hiển thị tại vị trí đó. Càng nhỏ càng đc ưu tiên</summary>
        public int priority;
    }
}