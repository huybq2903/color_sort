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
    /// Store giả dùng để test hoặc chạy trong Unity Editor.
    /// Không cài đặt kết nối thật đến store.
    /// </summary>
    public class PurchaseFakeStoreHandler : IPurchaseStoreHandler
    {
        public void OnInitialized(IExtensionProvider extensions)
        {
            //Không cần làm gì
        }
    }
}
