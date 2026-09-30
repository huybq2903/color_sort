/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-25
 */

using System;
using Falcon.Modules.Core.BigData;
using Falcon.Modules.Core.InAppPurchase.Runtime;
using Falcon.Modules.Level.Core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Falcon.Modules.InAppLog.Bigdata.Runtime
{
    public class BigdataLogger : IPurchaseLog
    {
        public static bool IsContainWhyInProductId { get; set; }

        public static Func<InAppParam> CreateDefaultInAppParam { get; set; }

        public static void CustomInAppParam(InAppParam param)
        {
            _param = param;
        }

        private static InAppParam _param;

        public void OnPurchaseStarted(Product product, string where, string why)
        {
            FalconBigDataController.Iap.OnStarted(
                BuildProductId(product.definition.id, why), where,
                product.metadata.localizedPrice, product.metadata.isoCurrencyCode);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            FalconBigDataController.Iap.OnFailed(MapFailReason(reason));
        }

        public void OnPurchaseDeferred(Product product)
        {
            FalconBigDataController.Iap.OnFailed(IapPurchaseFailReason.Pending, product.transactionID);
        }

        private static string BuildProductId(string productId, string why)
        {
            return IsContainWhyInProductId && !string.IsNullOrEmpty(why) ? productId + "_" + why : productId;
        }

        private static IapPurchaseFailReason MapFailReason(PurchaseFailureReason reason) => reason switch
        {
            PurchaseFailureReason.UserCancelled => IapPurchaseFailReason.UserCanceled,
            PurchaseFailureReason.ProductUnavailable => IapPurchaseFailReason.ItemUnavailable,
            PurchaseFailureReason.PurchasingUnavailable => IapPurchaseFailReason.BillingUnavailable,
#if FALCON_IAP_5_OR_NEWER
            PurchaseFailureReason.StoreNotConnected => IapPurchaseFailReason.BillingUnavailable,
#endif
            PurchaseFailureReason.DuplicateTransaction => IapPurchaseFailReason.DeveloperError,
            PurchaseFailureReason.ExistingPurchasePending => IapPurchaseFailReason.DeveloperError,
            _ => IapPurchaseFailReason.Unknown
        };

        public void Log(APurchaseProcess purchaseProcess)
        {
            var productID = purchaseProcess.PurchasedProduct.definition.id;
            var currencyCode = purchaseProcess.PurchasedProduct.metadata.isoCurrencyCode;
            var purchasePrice = purchaseProcess.PurchasedProduct.metadata.localizedPrice;
            var transactionId = purchaseProcess.PurchasedProduct.transactionID;
            var purchaseToken = string.Empty;
            var currentLevel = LevelData.Instance.level;

            if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
            {
                var r = JsonUtility.FromJson<ReceiptData>(purchaseProcess.PurchasedProduct.receipt);
                var p = JsonUtility.FromJson<PayloadData>(r.Payload);
                var pj = JsonUtility.FromJson<PayloadJsonData>(p.json);
                transactionId = pj.orderId;
                purchaseToken = pj.purchaseToken;
            }

            productID = BuildProductId(productID, purchaseProcess.Why);

            var inappParam = _param ?? CreateDefaultInAppParam?.Invoke() ?? new InAppParam();
            inappParam.productId = productID;
            inappParam.localizedPrice = purchasePrice;
            inappParam.isoCurrencyCode = currencyCode;
            inappParam.where = purchaseProcess.Where;
            inappParam.transactionId = transactionId;
            inappParam.purchaseToken = purchaseToken;
            inappParam.currentLevel = currentLevel;
            FalconBigDataController.Iap.OnPurchased(inappParam);
            _param = null;
        }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            IAPManager.onPurchaseFailed += _ => _param = null;
        }
    }

    public struct ReceiptData
    {
        public string Payload;
        public string Store;
        public string TransactionID;
    }

    public struct PayloadData
    {
        public string json;
        public string signature;
        public string skuDetails;
    }

    public struct PayloadJsonData
    {
        public string orderId;
        public string packageName;
        public string productId;
        public float purchaseTime;
        public int purchaseState;
        public string purchaseToken;
        public int quantity;
        public bool acknowledged;
    }
}