/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bắn log mốc mở app (hợp đồng §B): Cold lúc khởi tạo, Hot mỗi lần quay lại từ background
    /// nếu nền đủ lâu (ngưỡng lấy từ remote config). Dev game không phải gọi gì — trừ nguồn mở app
    /// (<see cref="ReportOpenSource"/>) vì Unity không nhìn thấy intent/launchOptions.
    /// </summary>
    [NoLazy]
    public class AppOpenLogService : MySingleton<AppOpenLogService>, IInit, IPioneer, ITerminal
    {
        private readonly AppOpenState _state = new();

        // Bên ghi vòng đời (Init / OnPreContinue / OnPostStop) chạy trên main thread, nhưng
        // ReportOpenSource do module push/deeplink gọi — callback notification không đảm bảo cùng
        // thread. Hai bên đụng chung mấy field state nên khoá cho chắc.
        private readonly object _lock = new();
        private readonly ITimeRepository _timeRepository;
        private readonly LogScheduleService _logScheduleService;
        private readonly AnalyticConfigService _configService;

        public AppOpenLogService(
            ITimeRepository timeRepository, LogScheduleService logScheduleService,
            AnalyticConfigService configService)
        {
            _timeRepository = timeRepository;
            _logScheduleService = logScheduleService;
            _configService = configService;
        }

        public Task Init(CancellationToken cancellationToken = default)
        {
            LogOpen();
            return Task.CompletedTask;
        }

        public void OnPreContinue()
        {
            LogOpen();
        }

        public void OnPostStop()
        {
            lock (_lock) _state.MarkPause(_timeRepository.CurrentTimeMillis);
        }

        /// <summary>
        /// Module push/deeplink báo lần mở app này đến từ đâu (§B4) — Unity không đọc được
        /// intent/launchOptions nên SDK-core không tự biết. Phải báo TRƯỚC khi bản tin app_open
        /// bắn: lần Cold thì bản tin bắn ngay trong pha init, nên chỗ gọi đúng là
        /// <c>RuntimeInitializeOnLoadMethod</c> hoặc <c>IPioneer</c> của module đó.
        /// </summary>
        public void ReportOpenSource(
            OpenSource source, string pushCampaignId = null,
            Dictionary<string, object> extraMeta = null)
        {
            bool late;
            lock (_lock)
                late = _state.ReportSource(source, pushCampaignId, _timeRepository.CurrentTimeMillis, extraMeta);
            if (late)
                AnalyticLogger.Instance.Warning(
                    $"ReportOpenSource({source}) gọi SAU khi bản tin app_open đã bắn — lần mở này " +
                    "không mang được nguồn (bản tin đã gửi thì không sửa được). Hãy gọi sớm hơn: " +
                    "RuntimeInitializeOnLoadMethod cho ca Cold, ngay trong callback nhận notification cho ca Hot.");
        }

        private void LogOpen()
        {
            AppOpenParam param;
            lock (_lock)
                param = _state.TryOpen(_timeRepository.CurrentTimeMillis, _configService.AppOpenMinBackgroundSec);
            if (param == null) return;
            param.networkType = NetworkTypeExtensions.Current();
            // Chỉ Cold mới có nghĩa: Hot là quay lại foreground, không có khởi động nào để đo
            if (param.launchType == LaunchType.Cold) param.startupDurationMs = StartupClock.ElapsedMillis;

            // Gọi được ngay cả trong Init: IInit chạy theo DependencyOrder mà service này phụ thuộc
            // LogScheduleService → LogDecorService → BaseLogDecorService → FCentralUserParamService
            // → các player repository, nên chúng đã init xong trước. (Central user params được chốt
            // vào DataWrapper ngay lúc Enqueue, không phải lúc flush — nên thứ tự này mới quan trọng.)
            _logScheduleService.Enqueue(new FAppOpenLog(param));
        }
    }
}
