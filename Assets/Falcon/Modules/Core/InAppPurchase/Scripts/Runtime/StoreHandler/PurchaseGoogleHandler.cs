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
    /// Xử lý giao dịch cho Google Play.
    /// Thiết lập extension và kiểm tra giao dịch bị hoãn.
    /// </summary>
    public class PurchaseGoogleHandler : IPurchaseStoreHandler
    {
        private IGooglePlayStoreExtensions _googleExtensions;

        public void OnInitialized(IExtensionProvider extensions)
        {
            _googleExtensions = extensions.GetExtension<IGooglePlayStoreExtensions>();
        }
        
        /// <summary>
        /// Kiểm tra giao dịch có đang ở trạng thái "hoãn" hay không.
        /// </summary>
        public bool IsPurchasedDeferred(Product product)
        {
            return _googleExtensions != null && _googleExtensions.IsPurchasedProductDeferred(product);
        }
    }
}
