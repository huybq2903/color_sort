/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-14
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.BigData;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.InAppPurchase.Runtime;
using Falcon.Modules.Packs.Core.Runtime;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Falcon.Modules.Packs.GameDataConnector.Runtimes
{
    public static class GameDataConnector
    {
        private static ABaseElementPackConfig currentPackInfo;
        private static Dictionary<string, object> detailTemp = new();
        private static string purchaseProductId;
        private static string purchasePlacement;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            GameEvent<ABaseElementPackConfig>.Register(PacksConstant.EVENT_BUY_SUCCESS_ADD_RESOURCES, OnBuySuccessAddResources, null);
            GameEvent<(string productId, Action success, Action failure, string where, string why)>.Register(PacksConstant.EVENT_PURCHASE, OnPurchaseStart, null);
            IAPManager.onPurchaseSuccess += OnBuySuccess;
        }

        private static void OnPurchaseStart((string productId, Action success, Action failure, string where, string why) purchase)
        {
            purchaseProductId = purchase.productId;
            purchasePlacement = purchase.where;
        }

        private static void OnBuySuccess(Product product)
        {
            if (currentPackInfo == null || product.definition.id != currentPackInfo.productId)
            {
                currentPackInfo = null;
                return;
            }
            var transactionId = GetTransactionId(product);
            detailTemp.Clear();
            detailTemp["transaction_id"] = transactionId;
            detailTemp["pack"] = currentPackInfo.idPack;
            var context = new ResourceParam
            {
                resourceWhen = FResourceWhen.Iap,
                resourceWhere = purchaseProductId == product.definition.id && !string.IsNullOrEmpty(purchasePlacement) ? purchasePlacement : "unknown",
                transactionId = transactionId,
            };
            foreach (var reward in currentPackInfo.rewards)
            {
                ResourceCollector.Instance.ResourceAdd(reward.name, reward.amount, reward.data, "buy_iap", currentPackInfo.productId, detailTemp, context);
            }
            ResourceCollector.Instance.SaveResourcesAndUpdateToServer();
            currentPackInfo = null;
            purchaseProductId = null;
            purchasePlacement = null;
        }


        private static string GetTransactionId(Product product)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                // Android: transactionID của Unity IAP là purchaseToken, phải lấy orderId mới khớp log IAP trên bigdata.
                return GooglePurchaseInfo.From(product.receipt).orderId;
            }
            catch (Exception e)
            {
                // Parse lỗi không được chặn việc cộng thưởng.
                Debug.LogException(e);
            }
#endif
            return product.transactionID;
        }

        private static void OnBuySuccessAddResources(ABaseElementPackConfig config) => currentPackInfo = config;

        /// <summary>Bóc thông tin giao dịch Google Play từ receipt của Unity IAP.</summary>
        [Serializable]
        private struct GooglePurchaseInfo
        {
            public string orderId;
            public string packageName;
            public string purchaseToken;

            public static GooglePurchaseInfo From(string receipt)
            {
                var payload = JsonUtility.FromJson<Receipt>(receipt).Payload;
                return JsonUtility.FromJson<GooglePurchaseInfo>(JsonUtility.FromJson<Payload>(payload).json);
            }

            [Serializable] private struct Receipt { public string Payload; }
            [Serializable] private struct Payload { public string json; }
        }
    }
}