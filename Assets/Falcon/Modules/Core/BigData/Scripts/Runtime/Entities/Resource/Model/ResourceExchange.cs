/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Gom các vế của MỘT giao dịch tài nguyên về chung một <c>exchangeId</c> (§D10).
    /// <br/>Không phải entity: giao dịch là một khoảnh khắc, không có vòng đời để giữ state —
    /// hàm thuần, id sinh xong là xong.
    /// </summary>
    public static class ResourceExchange
    {
        /// <summary>
        /// Set <see cref="FlowType"/> theo vế rồi đóng dấu chung một <c>exchangeId</c> lên tất cả.
        /// Vế nào đã tự mang exchangeId (vd id giao dịch của game server) thì lấy chính giá trị
        /// đó làm id chung, không có thì sinh mới. Trả về danh sách theo thứ tự sinks → sources.
        /// </summary>
        public static List<ResourceParam> Combine(
            IEnumerable<ResourceParam> sinks, IEnumerable<ResourceParam> sources)
        {
            var all = Materialize(sinks, FlowType.Sink);
            all.AddRange(Materialize(sources, FlowType.Source));
            if (all.Count == 0) return all;

            var exchangeId = all.FirstOrDefault(p => !string.IsNullOrEmpty(p.exchangeId))?.exchangeId
                             ?? Guid.NewGuid().ToString();
            foreach (var param in all) param.exchangeId = exchangeId;
            return all;
        }

        private static List<ResourceParam> Materialize(IEnumerable<ResourceParam> source, FlowType flowType)
        {
            if (source == null) return new List<ResourceParam>();
            var list = source.Where(p => p != null).ToList();
            foreach (var param in list) param.flowType = flowType;
            return list;
        }
    }
}
