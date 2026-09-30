/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Giữ vòng đời + cache tham số của lượt chơi level (state machine thuần nằm trong
    /// <see cref="LevelTurnState"/>) — xem EntityLifecycle-Design.md §2.
    /// Là nguồn chân lý DUY NHẤT về turn đang mở: decor service điền tham số qua đây,
    /// TurnCustomInfoRepository đọc id để stamp cross-entity, controller suspend/resume ván dở.
    /// </summary>
    public class LevelTurnService : MySingleton<LevelTurnService>
    {
        private readonly LevelTurnState _turnState = new();
        private readonly object _lock = new();
        private readonly EntityLabelService _entityLabelService;

        public LevelTurnService(EntityLabelService entityLabelService)
        {
            _entityLabelService = entityLabelService;
        }

        /// <summary>playTurnId của turn ĐANG MỞ (null nếu không có) — dùng stamp cross-entity.</summary>
        public string OpenPlayTurnId
        {
            get
            {
                lock (_lock) return _turnState.OpenPlayTurnId;
            }
        }

        /// <summary>
        /// Đưa param của một level log qua state machine: Start mở turn + seed cache,
        /// các status khác được điền tham số còn trống từ cache (giá trị đã nhập luôn thắng).
        /// Chuỗi bất thường chỉ cảnh báo lúc dev (compile-out ở release), không chặn log.
        /// </summary>
        public void Apply(ALevelParamV2 param)
        {
            LevelTurnState.Anomaly anomaly;
            lock (_lock)
            {
                anomaly = _turnState.Apply(param);
            }

            switch (anomaly)
            {
                case LevelTurnState.Anomaly.StartWhileOpen:
                    AnalyticLogger.Instance.Warning(
                        "Level Start arrived while previous turn still open — new turn overrides (check instrumentation order)");
                    break;
                case LevelTurnState.Anomaly.EventWithoutStart:
                    AnalyticLogger.Instance.Warning(
                        $"Level {param.Status} arrived without Start — playTurnId generated in place (check instrumentation order)");
                    break;
                case LevelTurnState.Anomaly.DuplicateTerminal:
                    AnalyticLogger.Instance.Warning(
                        $"Level {param.Status} lặp lại sau khi lượt đã đóng — nghi double-fire, giữ " +
                        "playTurnId cũ. Nếu đây là ván CHƠI LẠI thật thì game đang thiếu OnStart " +
                        "cho ván mới (mỗi lượt chơi một OnStart).");
                    break;
            }
        }

        /// <summary>Người chơi rời ván dở — xem LevelTurnState.Suspend. Null nếu không có turn mở.</summary>
        public LevelTurnSnapshot SuspendTurn()
        {
            lock (_lock) return _turnState.Suspend();
        }

        /// <summary>Đọc thuần bộ tham số ván đang chơi (autosave) — xem LevelTurnState.TakeSnapshot.</summary>
        public LevelTurnSnapshot CurrentSnapshot()
        {
            lock (_lock) return _turnState.TakeSnapshot();
        }

        /// <summary>
        /// Cập nhật tham số cấu hình của ván đang chơi — xem LevelTurnState.Update.
        /// Không có ván nào mở thì cảnh báo và bỏ qua: sửa cache của một lượt không tồn tại chỉ
        /// làm bẩn lượt sau.
        /// </summary>
        public void UpdateTurn(string difficulty = null, int? movesLimit = null, int? timeLimitSec = null)
        {
            bool updated;
            LevelTurnSnapshot snapshot;
            lock (_lock)
            {
                updated = _turnState.Update(difficulty);
                snapshot = _turnState.TakeSnapshot();
            }

            if (!updated)
            {
                AnalyticLogger.Instance.Warning(
                    "UpdateLevelTurn gọi lúc không có lượt chơi nào đang mở — bỏ qua " +
                    "(tham số của lượt chỉ có nghĩa trong lượt; muốn mở lượt mới thì gọi OnLevelStart).");
                return;
            }

            SetLevelConfig(snapshot.currentLevel, movesLimit, timeLimitSec);
        }

        /// <summary>
        /// Ghi config của BẢN THIẾT KẾ MÀN vào kho nhãn dưới tên hợp đồng (§H6: attribute của màn
        /// không thả flat lên bản tin lượt). Cửa duy nhất — mọi đường nhận hai số này đều đi qua đây.
        /// <br/>Không nhập thì KHÔNG gỡ nhãn cũ: màn đã khai giới hạn từ lần chơi trước, lần này
        /// game không nhắc lại không có nghĩa là giới hạn biến mất.
        /// </summary>
        public void SetLevelConfig(int currentLevel, int? movesLimit, int? timeLimitSec)
        {
            var entityId = currentLevel.ToString();
            if (movesLimit.HasValue)
                _entityLabelService.SetRegistered(
                    LabelEntityKind.Level, entityId, FLevelLabelKey.MOVES_LIMIT, movesLimit.Value);
            if (timeLimitSec.HasValue)
                _entityLabelService.SetRegistered(
                    LabelEntityKind.Level, entityId, FLevelLabelKey.TIME_LIMIT_SEC, timeLimitSec.Value);
        }

        /// <summary>
        /// Mở lại turn của ván dở đã save (re-open sau restart app) — xem LevelTurnState.Restore.
        /// playTurnId rỗng thì bỏ qua (game mất id thì cứ OnLevelStart lại — chấp nhận vỡ theo luật A3.4).
        /// </summary>
        public void RestoreTurn(
            string playTurnId, int currentLevel, string currentLevelId = null, string difficulty = null,
            int? movesLimit = null, int? timeLimitSec = null)
        {
            if (string.IsNullOrEmpty(playTurnId))
            {
                AnalyticLogger.Instance.Warning("RestoreTurn ignored: playTurnId is null/empty — call OnLevelStart for a fresh turn instead");
                return;
            }

            LevelTurnState.Anomaly anomaly;
            lock (_lock)
            {
                anomaly = _turnState.Restore(playTurnId, currentLevel, currentLevelId, difficulty);
            }

            SetLevelConfig(currentLevel, movesLimit, timeLimitSec);
            if (anomaly == LevelTurnState.Anomaly.RestoreWhileOpen)
                AnalyticLogger.Instance.Warning("RestoreTurn overrode an open turn — check the game's resume flow");
        }
    }
}
