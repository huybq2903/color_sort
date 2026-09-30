/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Quản lý cho hệ thống IAP.
    /// Gồm các hàm khởi tạo, mua, khôi phục và lấy thông tin sản phẩm.
    /// </summary>
    public static class IAPManager
    {
        /// <summary>
        /// Thực thi khi có giao dịch thành công
        /// </summary>
        public static event Action<Product> onPurchaseSuccess;

        /// <summary>
        /// Thực thi khi có giao dịch được khôi phục ở dưới nền
        /// </summary>
        public static event Action<Product> onPurchaseRestoreInBackground;

        /// <summary>
        /// Thực thi khi phát hiện bất thươờng
        /// </summary>
        public static event Action<Product> onHackDetected;

        /// <summary>
        /// Thực thi khi hết thời gian chờ xác thức
        /// </summary>
        public static event Action<Product> onPurchaseTimeOut;

        /// <summary>
        /// Thực thi khi giao dịch thất bại
        /// </summary>
        public static event Action<PurchaseFailureReason> onPurchaseFailed;

        /// <summary>
        /// Thực thi khi khởi tạo InApp thành công
        /// </summary>
        public static event Action OnInitializedSuccess;

        public static bool IsProductHavingReceipts(string productId)
        {
            return PurchaseListener.IsProductHavingReceipts(productId);
        }

        /// <summary>
        /// InApp đã khởi tạo xong chưa
        /// </summary>
        public static bool IsInitializedSuccess => PurchaseListener.InitSuccess;

        internal static void OnInitSuccessInternal() => OnInitializedSuccess?.Invoke();
        internal static void OnPurchaseSuccess(Product product) => onPurchaseSuccess?.Invoke(product);
        internal static void OnPurchaseRestoreInBackground(Product product) => onPurchaseRestoreInBackground?.Invoke(product);
        internal static void OnHackDetected(Product product) => onHackDetected?.Invoke(product);
        internal static void OnPurchaseTimeOut(Product product)
        {
            onPurchaseTimeOut?.Invoke(product);
            GameEvent.Emit(IAPConstant.EVENT_PURCHASE_TIME_OUT);
        }

        internal static void OnPurchaseFailed(PurchaseFailureReason reason)
        {
            onPurchaseFailed?.Invoke(reason);
            GameEvent.Emit(IAPConstant.EVENT_PURCHASE_FAIL);
        }

        /// <summary>
        /// ltv của người chơi
        /// </summary>
        public static double Ltv
        {
            get => SaveLoadHandler.Load<double>("ltv_iap");
            set => SaveLoadHandler.Save("ltv_iap", value);
        }

        /// <summary>
        /// Danh sách các đối tượng xác thực giao dịch (validator) đã được đăng ký.
        /// </summary>
        public static List<IPurchaseValidation> listPurchaseValidation;

        /// <summary>
        /// Danh sách các đối tượng log giao dịch đã đăng kí
        /// </summary>
        public static List<IPurchaseLog> listPurchaseLogger;

        private static PurchaseListener PurchaseListener { get; set; } = new();

        internal static void LoadAllValidationsAndLoggers(SOInAppPurchaseConfig so)
        {
            listPurchaseValidation = new List<IPurchaseValidation>();
            listPurchaseLogger = new List<IPurchaseLog>();

            foreach (var t in so.validations)
            {
                if (!t.enabled || string.IsNullOrEmpty(t.assemblyQualifiedTypeName)) continue;
                var type = Type.GetType(t.assemblyQualifiedTypeName);
                if (type != null && Activator.CreateInstance(type) is IPurchaseValidation val)
                {
                    val.Initialized();
                    listPurchaseValidation.Add(val);
                }
            }

            foreach (var t in so.loggers)
            {
                if (!t.enabled || string.IsNullOrEmpty(t.assemblyQualifiedTypeName)) continue;
                var type = Type.GetType(t.assemblyQualifiedTypeName);
                if (type != null && Activator.CreateInstance(type) is IPurchaseLog logger)
                {
                    listPurchaseLogger.Add(logger);
                }
            }
        }

        internal static void Initialize() => PurchaseListener.Initialize();

        private static bool SkipLog => listPurchaseLogger == null || (!IsLogPurchaseInEditor && Application.isEditor);

        internal static void LogPurchaseStarted(Product product, string where, string why)
        {
            if (SkipLog) return;
            foreach (var logger in listPurchaseLogger) logger.OnPurchaseStarted(product, where, why);
        }

        internal static void LogPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            if (SkipLog) return;
            foreach (var logger in listPurchaseLogger) logger.OnPurchaseFailed(product, reason);
        }

        internal static void LogPurchaseDeferred(Product product)
        {
            if (SkipLog) return;
            foreach (var logger in listPurchaseLogger) logger.OnPurchaseDeferred(product);
        }

        /// <summary>
        /// Thực hiện giao dịch
        /// </summary>
        /// <param name="productId">productId ứng với giao dịch</param>
        /// <param name="success">callback khi thành công</param>
        /// <param name="failure">callback khi thất bại, mặc định null</param>
        /// <param name="where">vị trí gọi giao dịch trong UI/game, mặc định null</param>
        /// <param name="why"> nhãn tuỳ chọn để phân biệt các giao dịch nếu dùng lại productId</param>
        public static void Purchase(string productId, Action success, Action failure = null, string where = null, string why = null)
        {
            PurchaseListener.Purchase(productId, success, failure, where, why);
        }

        public static void Purchase(string productId, Action<Product> success, Action failure = null, string where = null, string why = null)
        {
            PurchaseListener.Purchase(productId, success, failure, where, why);
        }

        /// <summary>
        /// Khôi phục các giao dịch cũ (dành cho IOS)
        /// </summary>
        public static void RestorePurchase(Action<RestoreResult> callback = null) => PurchaseListener.RestorePurchase(callback);

        /// <summary>
        /// Lấy metadata của sản phẩm theo ID.
        /// </summary>
        public static ProductMetadata GetProductMetadata(string productID) => PurchaseListener.GetProductMetadata(productID);

        /// <summary>
        /// Lấy giá hiển thị đã được địa phương hóa theo ID, trả về string.
        /// </summary>
        public static string GetLocalizedPrice(string productID) => PurchaseListener.GetLocalizedPrice(productID);

        /// <summary>
        /// Lấy giá usd, trả về double.
        /// </summary>
        public static double GetUsdPriceDouble(string productID) => PurchaseListener.GetUsdPriceDouble(productID);

        public static async Task<ProductMetadata> GetProductMetadataAsync(string productID)
        {
            while (!IsInitializedSuccess)
            {
                await Task.Yield();
            }
            return PurchaseListener.GetProductMetadata(productID);
        }

        public static async Task<string> GetLocalizedPriceAsync(string productID)
        {
            while (!IsInitializedSuccess)
            {
                await Task.Yield();
            }
            return PurchaseListener.GetLocalizedPrice(productID);
        }

        public static async Task<double> GetUsdPriceDoubleAsync(string productID)
        {
            while (!IsInitializedSuccess)
            {
                await Task.Yield();
            }
            return PurchaseListener.GetUsdPriceDouble(productID);
        }

        public static SubscriptionInfo GetSubscriptionInfo(string productId)
        {
            return !IsInitializedSuccess ? null : PurchaseListener.GetSubscriptionInfo(productId);
        }

        internal static void Dispose()
        {
            PurchaseListener?.Dispose();
            PurchaseListener = null;
        }

        internal static bool IsAnyPurchaseValidation => listPurchaseValidation is { Count: > 0 };

        internal static bool IsPurchaseSuccessIfTimeout => PurchaseListener.IsValidateLocalIfTimeout;

        internal static bool IsLogPurchaseInEditor => PurchaseListener.IsLogPurchaseInEditor;

        /// <summary>
        /// Tạo GameObject chứa IAPInitializer.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static async void OnGameStart()
        {
            GameEvent<(string productId, Action success, Action failure, string where, string why)>.Register(IAPConstant.EVENT_PURCHASE, CallBackPurchase, null);
            GameEvent<(string productId, Action<Product> success, Action failure, string where, string why)>.Register(IAPConstant.EVENT_PURCHASE, CallBackPurchaseProduct, null);
            GameRequest<string, Task<string>>.Register(IAPConstant.EVENT_GET_LOCALIZED_PRICE, CallBackPriceTask, null);
            GameRequest<string, string>.Register(IAPConstant.EVENT_GET_LOCALIZED_PRICE, CallBackPrice);
            GameRequest<double>.Register(IAPConstant.EVENT_GET_LTV, GetLtv);
            await Task.Yield();
            new GameObject("IAPInitializer").AddComponent<IAPInitializerBehaviour>();
        }

        private static Task<string> CallBackPriceTask(string productId) => GetLocalizedPriceAsync(productId);

        private static double GetLtv() => Ltv;

        private static void CallBackPurchaseProduct((string productId, Action<Product> success, Action failure, string where, string why) obj) =>
            Purchase(obj.productId, obj.success, obj.failure, obj.where, obj.why);

        private static void CallBackPurchase((string productId, Action success, Action failure, string where, string why) callback) =>
            Purchase(callback.productId, callback.success, callback.failure, callback.where, callback.why);

        private static string CallBackPrice(string productId) => GetLocalizedPrice(productId);
    }
}