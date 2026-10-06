/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Xử lý quy trình khôi phục các sản phẩm đã mua.
    /// Gọi callback khi thành công, phát hiện gian lận hoặc bị huỷ.
    /// </summary>
    public class RestoreProcess : APurchaseProcess
    {
        public RestoreProcess()
        {
            Where = nameof(RestoreProcess);
            Why = nameof(RestoreProcess);
        }

        protected override void OnValidationSucceeded()
        {
            IAPManager.OnPurchaseRestoreInBackground(PurchasedProduct);
        }

        protected override void OnHackDetected()
        {
            IAPManager.OnHackDetected(PurchasedProduct);
        }

        protected override void OnPurchaseFailed()
        {
        }

        protected override void Log()
        {
        }
    }
}
