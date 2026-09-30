/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class PlayerSessionService : ITerminal, IPioneer, IFPlayerSessionService, IInit
    {
        public const string TOTAL_TIME_MODE = "USER_TOTAL_TIME";

        private readonly IFPlayerSessionRepository _sessionRepository;
        private readonly ITimeRepository _timeRepository;

        // Mọi KHOẢNG thời gian (stretch, session) đo bằng MonotonicClock, KHÔNG phải wall-clock
        // (loader audit 12/08): wall-clock thì user chỉnh giờ máy TIẾN giữa stretch (cheat
        // skip-timer) là total_play_time phồng vĩnh viễn, mà loader chỉ clamp được chiều tụt.
        // Wall-clock (_timeRepository) vẫn dùng cho MỐC — ba luật chọn đồng hồ nằm ở doc của
        // MonotonicClock, đọc trước khi "sửa" chỗ này.
        private TimeSpan _lastPause;
        private TimeSpan _sessionStartMark;

        /// <summary>
        /// Khác null nghĩa là app đang ở background — ĐÓNG BĂNG đồng hồ chơi.
        /// Hợp đồng §H5: total_play_time chỉ được chạy khi foreground.
        /// </summary>
        private TimeSpan? _pausedAt;

        public PlayerSessionService(IFPlayerSessionRepository sessionRepository, ITimeRepository timeRepository)
        {
            _sessionRepository = sessionRepository;
            _timeRepository = timeRepository;

            _lastPause = MonotonicClock.Elapsed;
            _sessionStartMark = MonotonicClock.Elapsed;
            SessionStartTime = _timeRepository.UtcNow();
        }

        public DateTime SessionStartTime { get; private set; }

        /// <summary>
        /// Tổng thời gian chơi tích luỹ. Lúc app đang ở background thì stretch cuối ĐÃ được
        /// <see cref="OnPostStop"/> cộng vào repository rồi — cộng thêm <see cref="TimeSinceLastPause"/>
        /// nữa là đếm đôi (mọi log bắn lúc app pause sẽ mang giá trị phồng).
        /// </summary>
        public TimeSpan TotalPlayTime => TimeSpan.FromSeconds(_sessionRepository.GetModeTotalSec(TOTAL_TIME_MODE)) +
                                         (_pausedAt.HasValue ? TimeSpan.Zero : TimeSinceLastPause);

        public long FirstLogInMillis => _sessionRepository.FirstLogInMillis;

        public int ActiveDays => _sessionRepository.ActiveDays;

        public int SessionId => _sessionRepository.SessionId;

        public string SessionUid { get; } = Guid.NewGuid().ToString();

        public DateTime FirstLogInDateTimeLocal => DateTime.UnixEpoch.AddMilliseconds(FirstLogInMillis).ToLocalTime();
        public DateTime FirstLoginDateLocal => FirstLogInDateTimeLocal.Date;

        public int Retention =>
            DateTime.Compare(_timeRepository.LocalNow().Date, FirstLoginDateLocal.Date) > 0
                ? (_timeRepository.LocalNow().Date - FirstLoginDateLocal.Date).Days
                : 0;

        public bool RetentionChanged  => _sessionRepository.RetentionChanged;
        /// <summary>
        /// Độ dài stretch foreground hiện tại. Khi app đã pause thì giá trị ĐÓNG BĂNG tại mốc pause
        /// (không chạy tiếp trong lúc ở background) — nhờ vậy thứ tự gọi giữa các ITerminal không
        /// ảnh hưởng: ai đọc trước hay sau đều ra đúng một con số.
        /// </summary>
        public TimeSpan TimeSinceLastPause => (_pausedAt ?? MonotonicClock.Elapsed) - _lastPause;
        public TimeSpan SessionTime => MonotonicClock.Elapsed - _sessionStartMark;

        public void OnPreContinue()
        {
            _lastPause = MonotonicClock.Elapsed;
            _pausedAt = null;
        }

        public void OnPostStop()
        {
            // AppFlowService đã chặn stop hai lần, nhưng OnApplicationQuit sau OnApplicationPause
            // vẫn có thể vào đây — cộng lần hai là phồng counter vĩnh viễn.
            if (_pausedAt.HasValue) return;

            _pausedAt = MonotonicClock.Elapsed;
            var currentSessionTime = TimeSinceLastPause;
            BaseSystemLogger.Instance.Info("Duration from last Pause : " + currentSessionTime);

            // Đây là writer DUY NHẤT của TOTAL_TIME_MODE — SessionLogService cố tình không cộng
            // cho mode này (xem chú thích ở đó): hai bên cùng cộng làm counter chạy 2× tốc độ thật.
            _sessionRepository.IncreaseModeTotalSec(TOTAL_TIME_MODE, (long)currentSessionTime.TotalSeconds);
        }

        public Task Init(CancellationToken cancellationToken = default)
        {
            _lastPause = MonotonicClock.Elapsed;
            _pausedAt = null;
            _sessionStartMark = MonotonicClock.Elapsed;
            SessionStartTime = _timeRepository.UtcNow();
            return Task.CompletedTask;
        }
    }
}