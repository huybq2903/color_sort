/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Trang trí level log trong pipeline gửi: điền tham số lượt chơi từ cache
    /// (<see cref="LevelTurnService"/>) rồi tính các counter phái sinh (maxPassedLevel,
    /// failCount, playCount, win/lose streak).
    /// </summary>
    public class LevelLogDecorService : MySingleton<LevelLogDecorService>, ILogDecorator<FLevelLog>
    {
        private const string WIN_STREAK_KEY = "Level_Win_Streak";
        private const string LOSE_STREAK_KEY = "Level_Lose_Streak";

        private readonly LevelTurnService _turnService;
        private readonly EntityLabelService _entityLabelService;
        private readonly IFPlayerGeneralRepository _generalRepository;
        private readonly IDataPool _dataPool;
        private readonly ITimeRepository _timeRepository;
        private readonly object _streakLock = new();
        private string _lastStreakCountKey;

        public LevelLogDecorService(
            LevelTurnService turnService, EntityLabelService entityLabelService,
            IFPlayerGeneralRepository generalRepository,
            IDataPool dataPool, ITimeRepository timeRepository)
        {
            _turnService = turnService;
            _entityLabelService = entityLabelService;
            _generalRepository = generalRepository;
            _dataPool = dataPool;
            _timeRepository = timeRepository;
        }

        /// <summary>
        /// Điền tham số lượt chơi + tính counter cho một level log.
        /// Chạy trong pipeline decor (LogDecorService), decorate-once cho mỗi log instance.
        /// <br/>⚠ TUYỆT ĐỐI không tạo/gửi log bên trong hàm này (và trong LevelTurnState) —
        /// log mới sẽ lại đi qua đây và thành đệ quy vô hạn.
        /// </summary>
        public void Decor(FLevelLog log)
        {
            var logParams = log.param;

            // Điền tham số từ cache PHẢI chạy trước phần dưới vì LevelId phụ thuộc currentLevel/difficulty.
            _turnService.Apply(logParams);

            // Nhãn của MÀN đi ké level event (§D11) — dùng currentLevel CỦA CHÍNH LOG NÀY, không
            // phải màn đang mở: heartbeat trễ sau khi sang màn mới phải mang nhãn của màn nó tả.
            // Chạy sau Apply nên currentLevel đã được điền từ cache.
            log.levelLabels ??= _entityLabelService.BundleFor(
                LabelEntityKind.Level, logParams.currentLevel.ToString());

            var passedBefore = false;
            _dataPool.Compute<long>("HasPassedLevel_" + logParams.LevelId,
                value =>
                {
                    passedBefore = value.HasValue;
                    if (value.HasValue) return value;

                    if (!logParams.Status.IsPass()) return null;
                    // Math.Max chứ không gán thô (loader audit 12/08): pass lần đầu một màn SỐ NHỎ
                    // (side/bonus level) khi max đã 250 mà gán thô thì central param tụt về giá
                    // trị nhỏ trên MỌI log sau.
                    _generalRepository.MaxPassedLevel =
                        Math.Max(_generalRepository.MaxPassedLevel, logParams.currentLevel);
                    return _timeRepository.CurrentTimeSec();
                });

            // Hợp đồng §E: Re* statuses (ReplayPass/ReplayFail) đã khai tử, replay CŨNG gửi Start —
            // nên cờ này là thứ DUY NHẤT phân biệt "chơi lại màn đã qua" với "chơi lần đầu".
            // SDK tự điền vì mốc pass đầu vốn đã nằm trong pool (luật §H3: máy suy được thì máy điền).
            if (logParams.autoCheckComeBackAfterFirstPass && passedBefore)
                logParams.comeBackAfterFirstPass = true;

            // ReSharper disable once PossibleInvalidOperationException
            log.failCount = _dataPool.Compute<int>("Fail_Count_Level_" + logParams.LevelId,
                val =>
                {
                    val ??= 0;
                    return logParams.Status.IsFail() ? val + 1 : val;
                }).Value;
            // ReSharper disable once PossibleInvalidOperationException
            log.playCount = _dataPool.Compute<int>( "Play_Count_Level_" + logParams.LevelId,
                val =>
                {
                    val ??= 0;
                    return logParams.Status == LevelStatus.Start ? val + 1 : val;
                }).Value;

            UpdateAndStampStreaks(log);
        }

        /// <summary>
        /// Win/lose streak xuyên level, SDK tự tính (luật §H3 — giá trị máy suy được không giao tay dev):
        /// Pass → winStreak+1 và reset loseStreak; Fail → ngược lại; Skip/HeartBeat/Start không đụng streak.
        /// <br/>Ván DROP (bỏ ngang, không terminal nào) cũng KHÔNG đụng — cố ý: "bỏ = thua" là
        /// chính sách per-game chứ không phải bằng chứng, SDK suy hộ là chuỗi trên log lệch khỏi
        /// chuỗi trên UI của game. Game coi bỏ là thua thì báo Fail(failReason: Quit) — chuỗi reset
        /// tự nhiên và server được luôn fail_reason đúng vocab §D. Server muốn chuỗi-tính-cả-drop
        /// thì tự derive: turn không terminal nhìn thấy ngay trong stream.
        /// Start/HeartBeat mang streak TRƯỚC trận (vd "vào màn với 5 lần thua liên tiếp"),
        /// terminal mang streak ĐÃ TÍNH trận đó. Mỗi (turn, status) chỉ đếm 1 lần để
        /// double-fire terminal không làm lệch (Fail→revive→Pass cùng turn vẫn đếm cả hai vì khác status).
        /// </summary>
        private void UpdateAndStampStreaks(FLevelLog log)
        {
            var status = log.param.Status;
            if (status is LevelStatus.Pass or LevelStatus.Fail)
            {
                var countKey = log.param.playTurnId + "_" + status;
                lock (_streakLock)
                {
                    if (_lastStreakCountKey != countKey)
                    {
                        _lastStreakCountKey = countKey;
                        _dataPool.Compute<int>(WIN_STREAK_KEY, v => status.IsPass() ? (v ?? 0) + 1 : 0);
                        _dataPool.Compute<int>(LOSE_STREAK_KEY, v => status.IsFail() ? (v ?? 0) + 1 : 0);
                    }
                }
            }

            // ReSharper disable once PossibleInvalidOperationException
            log.winStreak = _dataPool.Compute<int>(WIN_STREAK_KEY, v => v ?? 0).Value;
            // ReSharper disable once PossibleInvalidOperationException
            log.loseStreak = _dataPool.Compute<int>(LOSE_STREAK_KEY, v => v ?? 0).Value;
        }

        /// <summary>
        /// Chuỗi THẮNG hiện tại (SDK tự tính, xuyên level + xuyên session). Đọc ở đây thay vì để
        /// game tự đếm lần hai: đếm hai nơi thì sớm muộn UI và log lệch nhau, mà lúc đó không ai
        /// biết bên nào đúng.
        /// <br/>Ván bỏ ngang không đụng chuỗi (xem <see cref="UpdateAndStampStreaks"/>) — game coi
        /// bỏ là thua thì báo <c>OnFail(failReason: Quit)</c>, đừng tự trừ tay.
        /// </summary>
        public int WinStreak => _dataPool.GetOrDefault(WIN_STREAK_KEY, 0);

        /// <inheritdoc cref="WinStreak"/>
        public int LoseStreak => _dataPool.GetOrDefault(LOSE_STREAK_KEY, 0);

        /// <summary>
        /// Reset winStreak về 0 — dành cho luật hết hạn của game (vd N ngày không chơi thì mất chuỗi).
        /// Chỉ có reset, KHÔNG có setter tùy ý: streak là giá trị SDK tự tính (luật §H3),
        /// cho set tay là mở cửa claim không bằng chứng.
        /// </summary>
        public void ResetWinStreak()
        {
            _dataPool.Compute<int>(WIN_STREAK_KEY, _ => 0);
        }

        /// <inheritdoc cref="ResetWinStreak"/>
        public void ResetLoseStreak()
        {
            _dataPool.Compute<int>(LOSE_STREAK_KEY, _ => 0);
        }
    }
}
