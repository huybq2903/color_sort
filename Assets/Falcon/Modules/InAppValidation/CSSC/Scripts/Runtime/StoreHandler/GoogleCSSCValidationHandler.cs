/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-18
 */

using Falcon.Modules.Core.InAppPurchase.Runtime;
using UnityEngine;

namespace Falcon.Modules.InAppValidation.CSSC.Runtime
{
    public class GoogleCSSCValidationHandler : IStoreCSSCValidationHandler
    {
        public void SendValidate(APurchaseProcess purchaseProcess)
        {
            Debug.Log($"Validating the purchase with the server on Google Play...\nreceipt: {purchaseProcess.PurchasedProduct.receipt}");
            var r = JsonUtility.FromJson<ReceiptData>(purchaseProcess.PurchasedProduct.receipt);
            var p = JsonUtility.FromJson<PayloadData>(r.Payload);
            var pj = JsonUtility.FromJson<PayloadJsonData>(p.json);

            new CSInappVerifyAndroid(purchaseProcess.PurchasedProduct.definition.id, pj.purchaseToken, pj.packageName)
                .AddSCListener<SCInappVerifyAndroidRsp>((message, timeout, success) =>
                {
                    // Khi timeout hoặc lỗi, FCallbackManager truyền message = null.
                    // Không nhận được phản hồi thì coi như giao dịch thành công.
                    if (message == null || timeout || !success)
                    {
                        purchaseProcess.OnReceiveValidation(APurchaseProcess.State.Purchased);
                        return;
                    }

                    switch (message.status)
                    {
                        case 0:
                            purchaseProcess.OnReceiveValidation(APurchaseProcess.State.Purchased); break;
                        case 1:
                            purchaseProcess.OnReceiveValidation(APurchaseProcess.State.Failed); break;
                    }
                }).Send();

            new CSGetLtvIap().Send();
        }

        public void Log(APurchaseProcess purchaseProcess)
        {
            var r = JsonUtility.FromJson<ReceiptData>(purchaseProcess.PurchasedProduct.receipt);
            var p = JsonUtility.FromJson<PayloadData>(r.Payload);
            var pj = JsonUtility.FromJson<PayloadJsonData>(p.json);
            var productID = purchaseProcess.PurchasedProduct.definition.id;
            new CSInappInfo
            {
                product_id = productID,
                placement = purchaseProcess.Where,
                localized_price = (double)purchaseProcess.PurchasedProduct.metadata.localizedPrice,
                iso_currency_code = purchaseProcess.PurchasedProduct.metadata.isoCurrencyCode,
                transaction_id = pj.orderId,
                purchase_token = pj.purchaseToken,
            }.Send();
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