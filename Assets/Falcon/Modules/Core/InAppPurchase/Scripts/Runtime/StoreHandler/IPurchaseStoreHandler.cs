/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using UnityEngine.Purchasing;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Interface dành cho các store handler tuỳ nền tảng.
    /// Dùng để xử lý khởi tạo và các extension liên quan.
    /// </summary>
    public interface IPurchaseStoreHandler
    {
        /// <summary>
        /// Được gọi khi hệ thống mua hàng được khởi tạo.
        /// Dùng để thiết lập extension theo nền tảng.
        /// </summary>
        void OnInitialized(IExtensionProvider extensions);
    }
}
