/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-09-05
 */

using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Kho gộp cụm exposure UI (icon/popup/banner event hiện ra và được bấm) — cùng triết lý cụm
    /// banner/multi-floor: per-lần-hiện là volume không chặn trên (5–30 lần/user/ngày × số
    /// surface × DAU = hàng triệu dòng/ngày — chốt owner 05/09 CẤM raw lên wire), gộp theo
    /// (surface × action) thì mỗi stretch chỉ còn vài dòng mang <c>count</c>, mà CTR theo ngày
    /// phía server vẫn chính xác tuyệt đối (sum/sum — server vẫn là người tính, client chỉ NÉN).
    /// <br/>Pure class — không IO/DI/khoá; <see cref="UiExposureClusterService"/> lo thread-safety.
    /// </summary>
    public class UiExposureState
    {
        /// <summary>Một cụm đã gộp — mỗi cụm thành một dòng <see cref="FUiImpressionLog"/>.</summary>
        public class Entry
        {
            public string surfaceId;
            public string uiAction;
            public int count;

            /// <summary>
            /// Extras của LẦN ĐẦU trong cụm — đại diện cho cả cụm (đừng nhét thứ đổi theo từng
            /// lần hiện; ngữ cảnh bất biến của surface như race_id thì hợp).
            /// </summary>
            public Dictionary<string, object> extraMeta;
        }

        private readonly Dictionary<(string surface, string action), Entry> _entries = new();

        public int Count => _entries.Count;

        public void Record(string surfaceId, string uiAction, Dictionary<string, object> extraMeta)
        {
            if (string.IsNullOrEmpty(surfaceId) || string.IsNullOrEmpty(uiAction)) return;

            var key = (surfaceId, uiAction);
            if (!_entries.TryGetValue(key, out var entry))
                _entries[key] = entry = new Entry
                {
                    surfaceId = surfaceId,
                    uiAction = uiAction,
                    extraMeta = extraMeta
                };

            entry.count++;
        }

        /// <summary>Rút toàn bộ cụm và làm sạch kho — mỗi lần flush là một thế hệ cụm mới.</summary>
        public List<Entry> TakeAll()
        {
            var result = new List<Entry>(_entries.Values);
            _entries.Clear();
            return result;
        }
    }
}
