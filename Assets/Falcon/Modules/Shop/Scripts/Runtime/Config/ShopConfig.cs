/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// Cấu hình tổng thể cho UI shop chính, chứa danh sách các pack.
    /// </summary>
    [Serializable]
    public class ShopConfig
    {
        public ShopElementConfig[] elementConfigs;
    }

    /// <summary>
    /// Thông tin từng pack trong UI shop chính.
    /// </summary>
    [Serializable]
    public class ShopElementConfig
    {
        /// <summary>ID duy nhất cho pack.</summary>
        public string idPack;
        
        /// <summary>Loại pack: Bundle, Currency,...</summary>
        [ValueDropdown(nameof(_typeDropdown))]
        public int type;
        
        /// <summary>Độ ưu tiên hiển thị trong nhóm.</summary>
        public int priority;
        
        private static IEnumerable<ValueDropdownItem<int>> _typeDropdown = new ValueDropdownItem<int>[]
        {
            new("Special", 1),
            new("Ads", 2),
            new("Bundle", 3),
            new("Currency", 4),
        };
    }
}