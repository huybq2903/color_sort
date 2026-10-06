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
    public class AppleCSSCValidationHadler : IStoreCSSCValidationHandler
    {
        public void SendValidate(APurchaseProcess purchaseProcess)
        {
            Debug.Log($"Validating the purchase with the server on Apple Store...\nreceipt: {purchaseProcess.PurchasedProduct.receipt}");

            new CSInappVerifyIos(
                    purchaseProcess.PurchasedProduct.definition.id,
                    purchaseProcess.PurchasedProduct.transactionID,
                    purchaseProcess.PurchasedProduct.receipt)
                .AddSCListener<SCInappVerifyIosRsp>((message, timeout, success) =>
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
        }

        public void Log(APurchaseProcess purchaseProcess)
        {
            var productID = purchaseProcess.PurchasedProduct.definition.id;
            new CSInappInfo
            {
                product_id = productID,
                placement = purchaseProcess.Where,
                localized_price = (double)purchaseProcess.PurchasedProduct.metadata.localizedPrice,
                iso_currency_code = purchaseProcess.PurchasedProduct.metadata.isoCurrencyCode,
                transaction_id = purchaseProcess.PurchasedProduct.transactionID,
                purchase_token = purchaseProcess.PurchasedProduct.receipt
            }.Send();

            new CSGetLtvIap().Send();
        }
    }
}