/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bắn cả bộ log của một giao dịch tài nguyên, mọi vế mang chung <c>exchangeId</c> (§D10).
    /// Logic gom vế nằm ở <see cref="ResourceExchange"/> — service chỉ lo cảnh báo dev-time và
    /// đẩy vào hàng đợi gửi.
    /// </summary>
    public class ResourceExchangeService : MySingleton<ResourceExchangeService>
    {
        private readonly LogScheduleService _logScheduleService;

        public ResourceExchangeService(LogScheduleService logScheduleService)
        {
            _logScheduleService = logScheduleService;
        }

        /// <summary>
        /// SDK tự set <see cref="FlowType"/> theo vế (sinks = <see cref="FlowType.Sink"/>,
        /// sources = <see cref="FlowType.Source"/>) nên game không cần điền.
        /// </summary>
        /// <param name="sinks">Các vế TRẢ ĐI (500 gold).</param>
        /// <param name="sources">Các vế NHẬN VỀ (3 booster).</param>
        public void LogExchange(IEnumerable<ResourceParam> sinks, IEnumerable<ResourceParam> sources)
        {
            var all = ResourceExchange.Combine(sinks, sources);
            if (all.Count == 0) return;

            if (all.Count < 2)
                AnalyticLogger.Instance.Warning(
                    "LogExchange chỉ nhận được 1 vế nên không có gì để ghép. Nếu vế còn lại đang " +
                    "bắn ở đường khác thì hai bên sẽ KHÔNG join được — gửi chung một lời gọi mới " +
                    "ăn exchangeId; còn nếu vốn dĩ chỉ có một vế thì dùng FResourceLog như cũ.");

            // Enqueue một lượt: cả bộ cùng vào hàng đợi, không có khe để app pause cắt đôi giao dịch
            _logScheduleService.EnqueueAll(all.Select(p => (IDataLog)new FResourceLog(p)));
        }

        /// <inheritdoc cref="LogExchange(IEnumerable{ResourceParam},IEnumerable{ResourceParam})"/>
        public void LogExchange(ResourceParam sink, ResourceParam source)
        {
            LogExchange(sink == null ? null : new[] { sink }, source == null ? null : new[] { source });
        }

        /// <summary>
        /// Bắn MỘT vế đứng riêng (thưởng cho không / tiêu mà thứ nhận về không đếm được) —
        /// không sinh <c>exchangeId</c> vì không có vế thứ hai để ghép; game tự set sẵn
        /// (vd id từ game server) thì giữ nguyên. <c>flowType</c> service set theo cửa gọi.
        /// </summary>
        public void LogSingle(ResourceParam param, FlowType flowType)
        {
            if (param == null) return;
            param.flowType = flowType;
            _logScheduleService.Enqueue(new FResourceLog(param));
        }
    }
}
