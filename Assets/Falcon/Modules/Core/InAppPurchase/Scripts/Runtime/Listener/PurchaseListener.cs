/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Falcon.Helpers.EventBus;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Xử lý toàn bộ tương tác với hệ thống Unity IAP, bao gồm khởi tạo,
    /// xử lý mua hàng, lỗi mua và khôi phục mua.
    /// </summary>
    public class PurchaseListener : IDetailedStoreListener, IDisposable
    {
        private readonly Dictionary<string, ProductInfo> _dictProductInfo = new();

        /// <summary>
        /// Dictionary chứa các giao dịch mới đang xử lý
        /// </summary>
        private readonly Dictionary<string, BuyProcess> _buyProcesses = new();

        /// <summary>
        /// Danh sách các giao dịch đợi khôi phục
        /// </summary>
        private readonly List<RestoreProcess> _restoreProcesses = new();

        private readonly IPurchaseStoreHandler _storeHandler;

        private IStoreController _storeController;
        private SOInAppPurchaseConfig _config;

        internal bool IsValidateLocalIfTimeout => _config.isValidateLocalIfTimeout;

        internal bool IsLogPurchaseInEditor => _config.isLogPurchaseInEditor;

        /// <summary>
        /// Khởi tạo đối tượng PurchaseListener với store handler tùy chọn (cho từng nền tảng).
        /// </summary>
        internal PurchaseListener(IPurchaseStoreHandler storeHandler = null)
        {
#if UNITY_EDITOR
            _storeHandler = storeHandler ?? new PurchaseFakeStoreHandler();
#elif UNITY_ANDROID
            _storeHandler = storeHandler ?? new PurchaseGoogleHandler();
#elif UNITY_IOS
            _storeHandler = storeHandler ?? new PurchaseAppstoreHandler();
#else
            _storeHandler = storeHandler ?? new PurchaseFakeStoreHandler();
#endif
        }

        /// <summary>
        /// Khởi tạo hệ thống mua hàng, nạp config, đăng ký sản phẩm và bắt đầu Unity Purchasing.
        /// </summary>
        internal async void Initialize()
        {
            try
            {
                var handle = Resources.LoadAsync<SOInAppPurchaseConfig>(IAPConstant.NAME_CONFIG);
                await handle;
                _config = handle.asset as SOInAppPurchaseConfig;
                if (!_config)
                {
                    Debug.LogError($"[PurchaseListener] IAP config not found at {IAPConstant.NAME_CONFIG}");
                    return;
                }

                var module = StandardPurchasingModule.Instance();
#if UNITY_EDITOR
                module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;
#endif

                var builder = ConfigurationBuilder.Instance(module);
                IAPManager.onPurchaseSuccess += AddLtv;
                IAPManager.LoadAllValidationsAndLoggers(_config);

                foreach (var product in _config.products)
                {
                    _dictProductInfo[product.productID] = product;
                    builder.AddProduct(product.productID, product.type);
                }

                if (UnityServices.State != ServicesInitializationState.Initialized &&
                    UnityServices.State != ServicesInitializationState.Initializing &&
                    !string.IsNullOrEmpty(_config.UnityServicesEnvironment))
                {
                    var options = new InitializationOptions().SetEnvironmentName(_config.UnityServicesEnvironment);
                    await UnityServices.InitializeAsync(options);
                }

                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void AddLtv(Product product)
        {
            var price = GetUsdPriceDouble(product.definition.id);
            if (price < 1) price = 1f;

            IAPManager.Ltv += price;
        }

        internal bool IsProductHavingReceipts(string productId)
        {
            if (!InitSuccess)
            {
                Debug.LogError("[PurchaseListener] IAP not initialized yet");
                return false;
            }
            var product = _storeController.products.WithID(productId);
            return product is { hasReceipt: true };
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _storeHandler.OnInitialized(extensions);
            IAPManager.OnInitSuccessInternal();
            Debug.Log("[PurchaseListener] IAP initialized successfully.");
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            var errorMessage = $"[{_storeHandler.GetType()}] Error initializing IAP: {error}.";
            if (!string.IsNullOrEmpty(message))
            {
                errorMessage += $" Detail: {message}";
            }
            Debug.LogError(errorMessage);
        }

        internal bool InitSuccess => _storeController != null;

        /// <summary>
        /// Thực hiện giao dịch mua với productId.
        /// </summary>
        internal void Purchase(string productId, Action success, Action failure, string where = null, string why = null)
        {
            var product = BeforePurchase(productId);
            if (product == null) return;

            Launch(product, new BuyProcess(success, failure, _config.purchaseTimeout, where, why), where, why);
        }

        internal void Purchase(string productId, Action<Product> success, Action failure, string where = null, string why = null)
        {
            var product = BeforePurchase(productId);
            if (product == null) return;

            Launch(product, new BuyProcess(success, failure, _config.purchaseTimeout, where, why), where, why);
        }

        private void Launch(Product product, BuyProcess process, string where, string why)
        {
            _buyProcesses[product.definition.id] = process;
            IAPManager.LogPurchaseStarted(product, where, why);
            GameEvent<object>.Emit(IAPConstant.EVENT_START_PURCHASE);
            GameEvent.Emit(IAPConstant.EVENT_START_PURCHASE);
            _storeController.InitiatePurchase(product);
        }

        private Product BeforePurchase(string productId)
        {
            if (!InitSuccess)
            {
                Debug.LogError("[PurchaseListener] Purchase called before IAP initialized.");
                return null;
            }

            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogError("Product ID cannot be null or empty.");
                return null;
            }

            var product = _storeController.products.WithID(productId);

            if (product is null or { availableToPurchase: false })
            {
                Debug.LogWarning($"[PurchaseListener] {productId} not registered yet");
                return null;
            }

            if (_buyProcesses.ContainsKey(productId))
            {
                Debug.LogWarning($"[PurchaseListener] Purchase already in progress for productID: {productId}");
                return null;
            }

            return product;
        }

        /// <summary>
        /// Xử lý sự kiện mua thành công từ Unity IAP.
        /// Thực hiện xác thực, xác nhận hoặc khôi phục.
        /// Trả về trạng thái Pending để chờ xác thực.
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            var product = purchaseEvent.purchasedProduct;
            var productId = product?.definition?.id;

            Debug.Log($"[{_storeHandler.GetType()}] > Processing Purchase: {productId}");

            if (productId == null) return PurchaseProcessingResult.Pending;

            if (_storeHandler is PurchaseGoogleHandler pgh && pgh.IsPurchasedDeferred(product))
            {
                IAPManager.LogPurchaseDeferred(product);
                return PurchaseProcessingResult.Pending;
            }

            if (_storeHandler is PurchaseAppstoreHandler pah)
                pah.SetHasProductForRestore(true);

            if (!string.IsNullOrEmpty(productId) && _buyProcesses.TryGetValue(productId, out var bp) && !bp.IsStarted)
            {
                bp.Start(product, p =>
                {
                    _storeController.ConfirmPendingPurchase(p);
                    _buyProcesses.Remove(productId);
                });

                return PurchaseProcessingResult.Pending;
            }

            if (!_restoreProcesses.Any(rp => rp.HasTransactionID(product.transactionID)))
            {
                var rp = new RestoreProcess();
                rp.Start(product, p =>
                {
                    _storeController.ConfirmPendingPurchase(p);
                    _restoreProcesses.Remove(rp);
                });
                _restoreProcesses.Add(rp);
            }

            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            IAPManager.OnPurchaseFailed(failureReason);
            OnPurchaseFailed(product, failureReason, failureReason.ToString());
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            if (failureDescription == null)
            {
                IAPManager.OnPurchaseFailed(PurchaseFailureReason.Unknown);
                OnPurchaseFailed(product, PurchaseFailureReason.Unknown, nameof(PurchaseFailureReason.Unknown));
                return;
            }

            IAPManager.OnPurchaseFailed(failureDescription.reason);
            OnPurchaseFailed(product, failureDescription.reason, failureDescription.message);
        }

        /// <summary>
        /// Xử lý lỗi mua.
        /// </summary>
        private void OnPurchaseFailed(Product product, PurchaseFailureReason reason, string msg)
        {
            var productId = product?.definition?.id;

            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogError("[PurchaseListener] Purchase failed: productID is null.");
                return;
            }

            if (_buyProcesses.Remove(productId, out var process))
            {
                Debug.LogError($"[PurchaseListener] Purchase failed: {msg}");
                IAPManager.LogPurchaseFailed(product, reason);
                process.OnForceFailed();
            }
            else
            {
                Debug.LogWarning($"[PurchaseListener] No BuyProcess found for failed productID: {productId}");
            }
        }

        /// <summary>
        /// Khôi phục các sản phẩm đã mua trước đó (thường dùng cho iOS).
        /// </summary>
        internal void RestorePurchase(Action<RestoreResult> callback)
        {
            if (!InitSuccess)
            {
                Debug.LogError("[PurchaseListener] IAP not initialized yet");
                return;
            }

            if (_storeHandler is PurchaseAppstoreHandler pah)
            {
                pah.RestorePurchase(callback);
            }
            else
            {
                callback?.Invoke(RestoreResult.FAILED);
                Debug.LogWarning("[PurchaseListener] RestorePurchase not supported on this platform/store handler.");
            }
        }

        /// <summary>
        /// Lấy metadata của sản phẩm theo ID.
        /// </summary>
        internal ProductMetadata GetProductMetadata(string productID)
        {
            if (!InitSuccess)
            {
                Debug.LogError("[PurchaseListener] IAP not initialized yet");
                return null;
            }
            return _storeController?.products.WithID(productID)?.metadata;
        }

        /// <summary>
        /// Lấy giá hiển thị đã được địa phương hóa theo ID.
        /// </summary>
        internal string GetLocalizedPrice(string productID)
        {
#if !UNITY_EDITOR
            var metaData = GetProductMetadata(productID);
            if (metaData != null)
            {
                var price = metaData.localizedPriceString;

                if (!string.IsNullOrEmpty(price))
                {
                    return price;
                }
            }
#endif
            if (!InitSuccess)
            {
                Debug.LogError("[PurchaseListener] IAP not initialized yet");
            }

            if (_dictProductInfo.TryGetValue(productID, out var product))
            {
                return product.defaultPrice;
            }

            return IAPConstant.DEFAULT_PRICE;
        }

        internal double GetUsdPriceDouble(string productID)
        {
            if (_dictProductInfo.TryGetValue(productID, out var product))
            {
                var numericPrice = Regex.Replace(product.defaultPrice, @"[^\d.]", "");
                if (double.TryParse(numericPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                {
                    return price;
                }
            }
            return 0.0;
        }

        internal SubscriptionInfo GetSubscriptionInfo(string productId)
        {
            var product = _storeController?.products.WithID(productId);
            return product is not { hasReceipt: true } ?
                null :
                new SubscriptionManager(product, null).getSubscriptionInfo();
        }

        /// <summary>
        /// Giải phóng các tài nguyên liên quan đến PurchaseListener.
        /// </summary>
        public void Dispose()
        {
            foreach (var bp in _buyProcesses.Values)
            {
                try { bp.ReleaseCts(); } catch (Exception ex) { Debug.LogException(ex); }
            }
            _buyProcesses.Clear();

            foreach (var rp in _restoreProcesses)
            {
                try { rp.ReleaseCts(); } catch (Exception ex) { Debug.LogException(ex); }
            }
            _restoreProcesses.Clear();
        }
    }
}