/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public class IapLogService : MySingleton<IapLogService>, ILogDecorator<FInAppLog>
    {
        private readonly IFPlayerGeneralRepository _generalRepository;
        private readonly IFPlayerIapRepository _iapRepository;

        // Inject CACHE chứ không phải service — cùng lý do AdLogService: service ôm
        // LogScheduleService là vòng chết DI, cache thì zero đường tới nó (luật ở ILogDecorator).
        private readonly OfferImpressionCache _offerCache;
        private readonly PurchaseAttemptCache _purchaseCache;

        public IapLogService(
            IFPlayerIapRepository iapRepository, IFPlayerGeneralRepository generalRepository,
            OfferImpressionCache offerCache, PurchaseAttemptCache purchaseCache)
        {
            _iapRepository = iapRepository;
            _generalRepository = generalRepository;
            _offerCache = offerCache;
            _purchaseCache = purchaseCache;
        }

        public void Decor(FInAppLog log)
        {
            var logParams = log.param;

            // ?? 0m chỉ là bọc kiểu: InAppParam.CorrectValues đã chặn null (và đã log lỗi) từ lúc
            // dựng log, nên tới đây giá trị luôn có.
            _iapRepository.RecordNewIap(logParams.productId, logParams.localizedPrice ?? 0m,
                logParams.isoCurrencyCode, _generalRepository.MaxPassedLevel);

            // Quy giao dịch về offer đang hiển thị nếu sản phẩm khớp (§C)
            log.offerImpressionId ??= _offerCache.TryAttributePurchase(logParams.productId);

            // PENDING cũ vừa hoàn tất (bẫy #4 §D4): khớp theo productId sẽ ăn nhầm lượt mua MỚI
            // cùng sản phẩm đang mở — giao dịch này để server nối bằng phễu, không gán attempt id.
            if (_purchaseCache.TryConsumePendingTransaction(logParams.transactionId)) return;

            // Nối vào phễu checkout: đóng lượt mua đang mở nếu khớp sản phẩm (§D4)
            log.purchaseAttemptId ??= _purchaseCache.TryAttributeSuccess(logParams.productId);
        }
    }
}