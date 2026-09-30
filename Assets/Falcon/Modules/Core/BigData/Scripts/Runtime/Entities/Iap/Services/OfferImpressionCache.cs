/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-12
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Kho state lần hiển thị offer — vai GIỮ STATE tách khỏi <see cref="OfferImpressionService"/>
    /// (vai bắn log) sau án deadlock DI 12/08, cùng lý do và cùng khuôn <see cref="AdViewCache"/>:
    /// decorator (<see cref="IapLogService"/>) chỉ cần đọc/quy giao dịch, không được rước
    /// <c>LogScheduleService</c> vào ctor.
    /// <br/>Khoá ở đây: bên ghi là UI game, bên đọc là pipeline decor lúc dựng log mua — callback
    /// billing không đảm bảo cùng thread.
    /// </summary>
    public class OfferImpressionCache : MySingleton<OfferImpressionCache>
    {
        private readonly OfferImpressionState _state = new();
        private readonly object _lock = new();
        private readonly ITimeRepository _timeRepository;

        public OfferImpressionCache(ITimeRepository timeRepository)
        {
            _timeRepository = timeRepository;
        }

        /// <summary>Id của offer ĐANG hiển thị (null nếu không có).</summary>
        public string OpenImpressionId
        {
            get { lock (_lock) return _state.OpenImpressionId; }
        }

        /// <summary>Ảnh chụp offer đang hiển thị (bản sao; null nếu không có).</summary>
        public OfferSnapshot TakeSnapshot()
        {
            lock (_lock) return _state.TakeSnapshot(_timeRepository.CurrentTimeMillis);
        }

        /// <summary>Mở lần hiển thị — trả (id vừa sinh, bản ghi offer trước bị đè cần log).</summary>
        public (string offerImpressionId, OfferImpressionRecord replaced) Open(IapOfferParam param, long graceMillis)
        {
            lock (_lock) return _state.Open(param, _timeRepository.CurrentTimeMillis, graceMillis);
        }

        /// <inheritdoc cref="OfferImpressionState.MarkClicked"/>
        public (OfferClick result, string offerImpressionId) MarkClicked()
        {
            lock (_lock) return _state.MarkClicked();
        }

        /// <summary>
        /// Giao dịch vừa xảy ra — trả offerImpressionId nếu sản phẩm khớp offer đang hiển thị
        /// (hoặc offer trong cửa sổ ân hạn). Decorator log mua gọi thẳng vào đây.
        /// </summary>
        public string TryAttributePurchase(string productId)
        {
            lock (_lock) return _state.TryAttribute(productId, _timeRepository.CurrentTimeMillis);
        }

        /// <summary>Đóng offer — trả bản ghi để log (null nếu không có gì/đã log lúc pause).</summary>
        /// <inheritdoc cref="OfferImpressionState.MergeExtraMeta"/>
        public void MergeExtraMeta(Dictionary<string, object> extraMeta)
        {
            lock (_lock) _state.MergeExtraMeta(extraMeta);
        }

        public OfferImpressionRecord Close(long graceMillis)
        {
            lock (_lock) return _state.Close(_timeRepository.CurrentTimeMillis, graceMillis);
        }

        /// <inheritdoc cref="OfferImpressionState.TakePauseSnapshot"/>
        public OfferImpressionRecord TakePauseSnapshot()
        {
            lock (_lock) return _state.TakePauseSnapshot(_timeRepository.CurrentTimeMillis);
        }
    }
}
