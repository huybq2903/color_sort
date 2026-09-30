/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-09-05
 */

using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Van volume cho exposure UI: nhận per-lần-hiện từ <see cref="FUiApi"/>, gộp cụm trong
    /// <see cref="UiExposureState"/>, flush lúc app pause qua <see cref="IAppPauseLogGenerator"/>
    /// — trăm lần hiện thành vài dòng mang <c>count</c> (khuôn cụm banner/multi-floor).
    /// <br/>KHÔNG persist qua kill: telemetry exposure, doanh thu/phễu không đứng trên nó —
    /// mất cụm dở của stretch bị kill cứng là cái giá chấp nhận, đổi lấy zero IO trên đường nóng
    /// (cùng phán quyết với cụm multi-floor).
    /// <br/><see cref="MAX_CLUSTERS"/> là van xả sớm: số surface × action bùng nổ ngoài dự kiến
    /// thì flush ngay thay vì ôm RAM tới pause.
    /// </summary>
    public class UiExposureClusterService : MySingleton<UiExposureClusterService>, IAppPauseLogGenerator
    {
        private const int MAX_CLUSTERS = 32;

        private readonly UiExposureState _state = new();
        private readonly object _lock = new();
        private readonly LogScheduleService _logScheduleService;

        public UiExposureClusterService(LogScheduleService logScheduleService)
        {
            _logScheduleService = logScheduleService;
        }

        public void Record(string surfaceId, string uiAction, Dictionary<string, object> extraMeta)
        {
            List<UiExposureState.Entry> overflow = null;
            lock (_lock)
            {
                _state.Record(surfaceId, uiAction, extraMeta);
                if (_state.Count >= MAX_CLUSTERS) overflow = _state.TakeAll();
            }

            if (overflow != null)
                _logScheduleService.EnqueueAll(overflow.Select(LogOf));
        }

        public IEnumerable<IDataLog> GetLogsOnAppPause()
        {
            List<UiExposureState.Entry> entries;
            lock (_lock) entries = _state.TakeAll();
            foreach (var entry in entries) yield return LogOf(entry);
        }

        private static IDataLog LogOf(UiExposureState.Entry entry)
        {
            return new FUiImpressionLog(entry);
        }
    }
}
