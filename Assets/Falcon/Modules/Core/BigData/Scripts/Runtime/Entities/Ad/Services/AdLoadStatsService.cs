/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-09-23
 */

using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Van volume cho telemetry load: nhận per-attempt từ <see cref="FAdApi.OnLoadResult"/>, gộp
    /// trong <see cref="AdLoadStatsState"/>, flush lúc app pause qua
    /// <see cref="IAppPauseLogGenerator"/> — hàng nghìn attempt thành vài dòng.
    /// <br/>KHÔNG persist qua kill (khác cụm banner): đây là telemetry chỉnh floor, doanh thu thật
    /// đã có f_sdk_ads_data lo — mất cụm dở của stretch bị kill cứng là cái giá chấp nhận, đổi lấy
    /// zero IO trên đường nóng.
    /// <br/><see cref="MAX_UNITS_IN_MEMORY"/> là van xả sớm khi khoá bùng nổ. Nâng 32 → 256 ở v2
    /// (23/09): trần cũ cắt vụn cụm giữa chừng, mà giờ một lần xả chỉ ra vài dòng mang mảng nên ôm
    /// nhiều khoá hơn là rẻ — mỗi khoá chỉ vài chục byte.
    /// <br/><see cref="MAX_UNITS_PER_LOG"/> là trần loader đặt cho mảng <c>units[]</c>; vượt thì
    /// CHIA LÔ thành nhiều dòng, không cắt bỏ.
    /// </summary>
    public class AdLoadStatsService : MySingleton<AdLoadStatsService>, IAppPauseLogGenerator
    {
        private const int MAX_UNITS_IN_MEMORY = 256;
        private const int MAX_UNITS_PER_LOG = 64;

        private readonly AdLoadStatsState _state = new();
        private readonly object _lock = new();
        private readonly LogScheduleService _logScheduleService;

        public AdLoadStatsService(LogScheduleService logScheduleService)
        {
            _logScheduleService = logScheduleService;
        }

        public void Record(AdLoadResultParam param)
        {
            if (param == null) return;

            List<AdLoadStatsState.Batch> overflow = null;
            lock (_lock)
            {
                _state.Record(param);
                if (_state.Count >= MAX_UNITS_IN_MEMORY) overflow = _state.TakeAll(MAX_UNITS_PER_LOG);
            }

            if (overflow != null)
                _logScheduleService.EnqueueAll(LogsOf(overflow));
        }

        public IEnumerable<IDataLog> GetLogsOnAppPause()
        {
            List<AdLoadStatsState.Batch> batches;
            lock (_lock) batches = _state.TakeAll(MAX_UNITS_PER_LOG);
            return LogsOf(batches);
        }

        private static List<IDataLog> LogsOf(List<AdLoadStatsState.Batch> batches)
        {
            var logs = new List<IDataLog>(batches.Count);
            foreach (var batch in batches) logs.Add(new FAdLoadStatsLog(batch));
            return logs;
        }
    }
}
