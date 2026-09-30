/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    [NoLazy]
    public class SessionCheckService : MySingleton<SessionCheckService>, IAppPauseLogGenerator, IInit
    {
        private const string LAST_RETENTION_CHECKED_KEY = "Analytic_Last_Retention_Check";
        private const double TICK_LOG_START_THRESHOLD_SEC = 60;
        private const double TICK_LOG_MAX_THRESHOLD_SEC = 16 * 60;
        private readonly IFPlayerSessionService _sessionService;
        private readonly ITimeRepository _timeRepository;
        private readonly LogScheduleService _logScheduleService;
        private readonly BasicPoolData<int> _lastRetentionChecked;

        private readonly object _totalTimeLogLock = new();
        private double _tickLoggedOfStretchSec;
        private double _tickLogThresholdSec = TICK_LOG_START_THRESHOLD_SEC;

        public SessionCheckService(
            IFPlayerSessionService sessionService, ITimeRepository timeRepository, IDataPool dataPool,
            LogScheduleService logScheduleService)
        {
            _sessionService = sessionService;
            _timeRepository = timeRepository;
            _logScheduleService = logScheduleService;
            _lastRetentionChecked = new (dataPool, LAST_RETENTION_CHECKED_KEY, -1);
        }

        public IEnumerable<IDataLog> GetLogsOnAppPause()
        {
            TimeSpan remainder;
            lock (_totalTimeLogLock)
            {
                // Chỉ log phần CHƯA được tick log gửi (tránh đếm đôi với log bảo hiểm định kỳ);
                // stretch kết thúc tại pause này nên phần đã tick log reset về 0.
                remainder = _sessionService.TimeSinceLastPause - TimeSpan.FromSeconds(_tickLoggedOfStretchSec);
                _tickLoggedOfStretchSec = 0;
            }
            if (remainder.TotalSeconds < 5) yield break;
            yield return WithPerfSummary(new FSessionLog(new SessionParam
            {
                gameMode = PlayerSessionService.TOTAL_TIME_MODE,
                sessionTime = remainder
            }));
            AnalyticLogger.Instance.Info("Duration from last Pause : " + remainder);
        }

        public Task Init(CancellationToken cancellationToken = default)
        {
            new RepeatAction(() =>
            {
                CheckRetention();
                TryLogTotalTimeOnTick();
            }, TimeSpan.FromMinutes(1)).Schedule();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Log USER_TOTAL_TIME bảo hiểm: bình thường log chỉ được tạo lúc app pause, nên app bị kill cứng
        /// (crash, hết pin, force stop) là mất trắng thời gian chơi của stretch hiện tại.
        /// Tick này gửi phần thời gian chơi chưa được log với khoảng cách lũy tiến (1, 2, 4, 8, 16 phút —
        /// tính từ log gần nhất bất kể loại) để kill cứng chỉ mất tối đa 1 khoảng threshold,
        /// đồng thời chỉ thêm ~2-3 log mỗi session. Log lúc pause trừ đi phần đã gửi ở đây nên tổng không đổi.
        /// </summary>
        private void TryLogTotalTimeOnTick()
        {
            TimeSpan delta;
            lock (_totalTimeLogLock)
            {
                var unloggedSec = _sessionService.TimeSinceLastPause.TotalSeconds - _tickLoggedOfStretchSec;
                if (unloggedSec < _tickLogThresholdSec) return;
                delta = TimeSpan.FromSeconds(unloggedSec);
                _tickLoggedOfStretchSec += unloggedSec;
                _tickLogThresholdSec = Math.Min(_tickLogThresholdSec * 2, TICK_LOG_MAX_THRESHOLD_SEC);
            }
            _logScheduleService.Enqueue(WithPerfSummary(new FSessionLog(new SessionParam
            {
                gameMode = PlayerSessionService.TOTAL_TIME_MODE,
                sessionTime = delta
            })));
            AnalyticLogger.Instance.Info("Interim total time log (kill-safe): " + delta);
        }

        /// <summary>
        /// Đóng tóm tắt hiệu năng vào bản tin session_data (§D7): zero event mới, zero volume mới.
        /// Mỗi bản tin mang số liệu của ĐÚNG quãng nó khai (lấy xong là reset) nên gộp lại không
        /// đếm đôi — kể cả bản tin bảo hiểm định kỳ, nhờ đó app bị kill cứng vẫn còn số liệu.
        /// </summary>
        private FSessionLog WithPerfSummary(FSessionLog log)
        {
            // Lấy qua Instance chứ KHÔNG inject qua ctor: PerfSampleService là MonoSingleton, mà
            // service này được dựng trong pha [NoLazy] — chưa chắc đã có GameObject để gắn vào.
            // Init lỗi thì InitService nuốt exception, hậu quả là log phiên lặng lẽ ngừng sinh.
            // Ở đây thì luôn an toàn: chỉ chạy lúc app pause / tick định kỳ, GameObject có chắc chắn.
            var perf = PerfSampleService.Instance.TakeSummary();
            log.avgFps = perf.AvgFps;
            log.frameDropCount = perf.FrameDropCount;
            log.memoryWarningCount = perf.MemoryWarningCount;
            return log;
        }

        public void CheckRetention()
        {
            _lastRetentionChecked.Compute(i =>
            {
                if (i == _sessionService.Retention) return i;
                
                _logScheduleService.Enqueue(new FRetentionLog(_timeRepository.LocalNow()));
                return _sessionService.Retention;
            });
        }
    }
}