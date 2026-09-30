/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Interface dành cho các hệ thống xác thực giao dịch.
    /// Có thể mở rộng để tích hợp server, Appsflyer, v.v.
    /// </summary>
    public interface IPurchaseValidation
    {
        void Initialized();
        /// <summary>
        /// Gửi yêu cầu xác thực cho tiến trình giao dịch.
        /// </summary>
        void SendValidate(APurchaseProcess purchaseProcess);
    }
}