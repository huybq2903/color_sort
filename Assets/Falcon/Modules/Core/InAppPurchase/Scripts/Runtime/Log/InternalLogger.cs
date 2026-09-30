/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-20
 */

using System;
using Falcon.Helpers.EventBus;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    internal class InternalLogger : IPurchaseLog
    {
        private const string EVENT_GET_LEVEL = "falcon.modules.core.gamedata.get_level";
        public void Log(APurchaseProcess purchaseProcess)
        {
            var localizePrice = purchaseProcess.PurchasedProduct.metadata.localizedPrice;
            var isoCurrencyCode = purchaseProcess.PurchasedProduct.metadata.isoCurrencyCode;
            var localizeDict = FInAppData.Instance.localizeData;
            if (!localizeDict.TryGetValue(isoCurrencyCode, out var data))
            {
                localizeDict[isoCurrencyCode] = new FInAppData.LocalizedData()
                {
                    count = 1,
                    isoCurrencyCode = isoCurrencyCode,
                    max = localizePrice,
                    total = localizePrice,
                };
            }
            else
            {
                data.count++;
                data.total += localizePrice;
                data.max = Math.Max(data.max, localizePrice);
            }

            var utcMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            FInAppData.Instance.firstRecord ??= new FInAppData.RecordData()
            {
                level = GameRequest<int>.Request(EVENT_GET_LEVEL),
                timestamp = utcMillis,
                productId = purchaseProcess.PurchasedProduct.definition.id,
            };
            FInAppData.Instance.lastRecord = new FInAppData.RecordData()
            {
                level = GameRequest<int>.Request(EVENT_GET_LEVEL),
                timestamp = utcMillis,
                productId = purchaseProcess.PurchasedProduct.definition.id,
            };
            
            FInAppData.Instance.UpdateToServer();
        }
    }
}