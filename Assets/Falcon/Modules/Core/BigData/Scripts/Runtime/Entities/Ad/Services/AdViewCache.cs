/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-12
 */

using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Kho state vòng đời ad view — TÁCH khỏi <see cref="AdRequestService"/> sau án deadlock DI
    /// 12/08: service gói hai vai (giữ state + bắn log), decorator chỉ cần vai giữ state nhưng
    /// inject cả cục là rước luôn <c>LogScheduleService</c> vào ctor → khép vòng
    /// Schedule→Decor→AdLogService→AdRequestService→Schedule.
    /// <br/>Class này là vai giữ state đứng riêng: chỉ ôm <see cref="AdViewState"/> (model thuần)
    /// + khoá + đồng hồ — ZERO đường tới LogScheduleService, nên decorator ctor-inject được như
    /// <c>LevelLogDecorService</c> vẫn inject <c>LevelTurnService</c> (cùng hình dạng này từ đầu).
    /// <br/>Khoá ở đây vì bên GHI là callback mediation, bên ĐỌC là pipeline decor — không đảm bảo
    /// cùng thread (RaiseAdEventsOnUnityMainThread có thể bị UMP tắt trong nhánh retry init).
    /// </summary>
    public class AdViewCache : MySingleton<AdViewCache>
    {
        private readonly AdViewState _state = new();
        private readonly object _lock = new();
        private readonly ITimeRepository _timeRepository;

        public AdViewCache(ITimeRepository timeRepository)
        {
            _timeRepository = timeRepository;
        }

        /// <summary>Mở vòng đời một lần xem ad — trả adViewId vừa sinh để caller gắn lên log.</summary>
        public string Open(AdViewParam param)
        {
            lock (_lock) return _state.Open(param, _timeRepository.CurrentTimeMillis);
        }

        /// <summary>Mốc gọi hiển thị — trả (adViewId, requestToShowMs, rotated) cho log show; rotated = SDK vừa xoay id vì view đã có impression (caller cảnh báo).</summary>
        public (string adViewId, int? requestToShowMs, bool rotated) MarkShown(AdShowParam param)
        {
            lock (_lock) return _state.MarkShown(param, _timeRepository.CurrentTimeMillis);
        }

        /// <inheritdoc cref="AdViewState.StampImpression"/>
        public (string adViewId, int? fillLatencyMs, bool rotated) StampImpression(AdType type)
        {
            lock (_lock) return _state.StampImpression(type, _timeRepository.CurrentTimeMillis);
        }

        /// <summary>Mốc đóng — trả (adViewId, shownDurationSec) cho log close.</summary>
        public (string adViewId, int? shownDurationSec) MarkClosed(AdCloseParam param)
        {
            lock (_lock) return _state.MarkClosed(param, _timeRepository.CurrentTimeMillis);
        }

        /// <inheritdoc cref="AdViewState.MarkClicked"/>
        public bool MarkClicked(AdType type)
        {
            lock (_lock) return _state.MarkClicked(type);
        }

        /// <summary>Id lần xem ad hiện tại của format (null nếu chưa có request) — xem AdViewState.CurrentId.</summary>
        public string CurrentViewId(AdType type)
        {
            lock (_lock) return _state.CurrentId(type);
        }

        /// <summary>Độ trễ fill (ms) từ lúc xin ad tới bây giờ — null nếu không đi từ request nào (§D2).</summary>
        public int? FillLatencyMs(AdType type)
        {
            lock (_lock) return _state.MillisSinceRequest(type, _timeRepository.CurrentTimeMillis);
        }

        /// <summary>Ghi chỗ chiếu/ngữ cảnh cho view hiện tại — false nếu format chưa có request.</summary>
        public bool SetContext(AdType type, string adWhere = null, string adWhen = null, string adMediation = null)
        {
            lock (_lock) return _state.SetContext(type, adWhere, adWhen, adMediation);
        }

        /// <summary>Luật hai chiều adWhere/adWhen/adMediation — xem AdViewState.ApplyContext.</summary>
        public void ApplyContext(AdViewParam param, AdType type)
        {
            lock (_lock) _state.ApplyContext(param, type);
        }

        /// <summary>Ảnh chụp view hiện tại của format (bản sao; null nếu chưa có request).</summary>
        public AdViewSnapshot TakeSnapshot(AdType type)
        {
            lock (_lock) return _state.TakeSnapshot(type, _timeRepository.CurrentTimeMillis);
        }
    }
}
