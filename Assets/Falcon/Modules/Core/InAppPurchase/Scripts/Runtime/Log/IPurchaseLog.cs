/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using UnityEngine.Purchasing;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Interface dành cho các hệ thống ghi log giao dịch.
    /// </summary>
    public interface IPurchaseLog
    {
        void Log(APurchaseProcess purchaseProcess);

        /// <summary>Ngay trước khi mở billing flow của store.</summary>
        void OnPurchaseStarted(Product product, string where, string why) { }

        /// <summary>Billing trả lỗi/huỷ cho một giao dịch đang mở.</summary>
        void OnPurchaseFailed(Product product, PurchaseFailureReason reason) { }

        /// <summary>Google trả giao dịch treo (deferred/PENDING) — chưa phải fail thật.</summary>
        void OnPurchaseDeferred(Product product) { }
    }
}