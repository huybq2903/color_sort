/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using UnityEngine.Purchasing;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Xử lý giao dịch cho App Store (iOS, macOS).
    /// Thiết lập extension, khôi phục mua và kiểm tra quyền thanh toán.
    /// </summary>
    public class PurchaseAppstoreHandler : IPurchaseStoreHandler
    {
        private IAppleExtensions _appleExtensions;
        private bool _hasProductForRestore;
        
        public void OnInitialized(IExtensionProvider extensions)
        {
            _appleExtensions = extensions.GetExtension<IAppleExtensions>();
        }
        
        public void SetHasProductForRestore(bool hasProductForRestore) => _hasProductForRestore = hasProductForRestore;
        
        /// <summary>
        /// Khôi phục giao dịch cũ cho người dùng trên nền tảng Apple.
        /// </summary>
        public void RestorePurchase(Action<RestoreResult> callback)
        {
            _hasProductForRestore = false;
            _appleExtensions.RestoreTransactions((result, error) =>
            {
                if (!result) {
                    callback?.Invoke(RestoreResult.FAILED);
                    return;
                }

                if (_hasProductForRestore)
                    callback?.Invoke(RestoreResult.SUCCESS);
                else
                    callback?.Invoke(RestoreResult.NO_PURCHASE);
            });
        }
    }
}
