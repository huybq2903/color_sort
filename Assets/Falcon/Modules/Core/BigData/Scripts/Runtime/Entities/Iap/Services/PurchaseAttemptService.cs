/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bắn log phễu checkout (mở lượt / thất bại) — §D4. State + sổ PENDING nằm ở
    /// <see cref="PurchaseAttemptCache"/> (tách riêng sau án deadlock 12/08); class này chỉ còn
    /// vai DỰNG BẢN TIN + cảnh báo lúc dev.
    /// </summary>
    public class PurchaseAttemptService : MySingleton<PurchaseAttemptService>
    {
        private readonly PurchaseAttemptCache _cache;
        private readonly LogScheduleService _logScheduleService;

        public PurchaseAttemptService(PurchaseAttemptCache cache, LogScheduleService logScheduleService)
        {
            _cache = cache;
            _logScheduleService = logScheduleService;
        }

        /// <summary>Id của lượt mua đang mở (null nếu không có) — cửa cũ, bản chất ở cache.</summary>
        public string OpenAttemptId => _cache.OpenAttemptId;

        /// <summary>Ảnh chụp lượt mua đang mở (null nếu không có).</summary>
        public PurchaseAttemptSnapshot CurrentSnapshot()
        {
            return _cache.TakeSnapshot();
        }

        /// <summary>
        /// Bắt đầu một lượt mua → log mốc mở phễu checkout. Service CHỈ nhận param — nén knob
        /// phẳng thành param là việc của <see cref="FIapApi"/> (một đường xử lý ở đây).
        /// </summary>
        public void StartPurchase(IapPurchaseAttemptParam param)
        {
            if (param == null) return;
            // networkType là SỐ ĐO của SDK, không phải cái game khai — stamp đè bất kể param
            param.networkType = NetworkTypeExtensions.Current();

            var opened = _cache.Open(param);
            if (opened.replaced)
                AnalyticLogger.Instance.Warning(
                    $"Purchase attempt for {param.productId} started while another was still open — previous attempt has no terminal event");

            _logScheduleService.Enqueue(new FIapStartPurchaseLog(param) { purchaseAttemptId = opened.attemptId });
        }

        /// <summary>
        /// Mua THÀNH CÔNG → log doanh thu (<c>f_sdk_in_app_data</c>). Chỉ enqueue — mọi hậu kỳ
        /// (sổ LTV, quy về offer đang hiển thị, bẫy PENDING, đóng phễu checkout) decorator
        /// <see cref="IapLogService"/> tự làm cho MỌI log đi qua pipeline, đường nào cũng vậy.
        /// </summary>
        public void LogPurchase(InAppParam param)
        {
            if (param == null) return;
            // networkType là SỐ ĐO của SDK — cùng luật với mốc mở và mốc fail
            param.networkType = NetworkTypeExtensions.Current();
            _logScheduleService.Enqueue(new FInAppLog(param));
        }

        /// <summary>Lượt mua kết thúc bằng thất bại/huỷ/treo → log fail mang cùng attempt id.</summary>
        /// <param name="pendingTransactionId">
        /// CHỈ cho <see cref="IapPurchaseFailReason.Pending"/>: transactionId của giao dịch đang
        /// treo (Google PENDING). Có nó thì khi giao dịch hoàn tất — có thể vài ngày sau, lúc một
        /// lượt mua MỚI cùng sản phẩm đang mở — SDK nhận ra "đây là cái pending cũ" và KHÔNG gán
        /// nhầm vào lượt mới (bẫy #4 §D4, loader audit 12/08).
        /// </param>
        public void FailPurchase(IapPurchaseFailReason reason, string pendingTransactionId = null)
        {
            if (reason == IapPurchaseFailReason.Pending)
                _cache.RegisterPendingTransaction(pendingTransactionId);

            var failed = _cache.TakeFail(reason);
            if (failed.param == null)
            {
                AnalyticLogger.Instance.Warning(
                    $"Purchase failed ({reason}) with no open attempt — call OnPurchaseStarted before launching the billing flow");
                return;
            }

            // Mạng đo TẠI MỐC THẤT BẠI (không lấy lại lúc mở) — mạng rớt giữa chừng chính là
            // thứ giải thích failReason = network.
            failed.param.networkType = NetworkTypeExtensions.Current();
            _logScheduleService.Enqueue(
                new FIapPurchaseFailLog(failed.param) { purchaseAttemptId = failed.attemptId });
        }
    }
}
