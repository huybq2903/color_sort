/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Dựng và gửi level log từ các khoảnh khắc gameplay (controller gọi xuống).
    /// Chỉ lo dựng param — tham số định danh của lượt chơi do LevelTurnService điền
    /// và counter do LevelLogDecorService tính, tất cả chạy trong pipeline decor lúc gửi.
    /// </summary>
    public class LevelLogService : MySingleton<LevelLogService>
    {
        private readonly LogScheduleService _logScheduleService;
        private readonly LevelTurnService _turnService;

        public LevelLogService(LogScheduleService logScheduleService, LevelTurnService turnService)
        {
            _logScheduleService = logScheduleService;
            _turnService = turnService;
        }

        /// <summary>Dựng LevelStartParamV2 và gửi log Start — mở turn mới, seed cache.</summary>
        public void LogLevelStart(
            LevelStartParamV2 param, int? movesLimit = null, int? timeLimitSec = null)
        {
            if (param == null) return;

            // movesLimit/timeLimitSec KHÔNG nằm trong param: chúng là config của BẢN THIẾT KẾ MÀN
            // (§H6 — chủ thể LEVEL), đi vào kho nhãn chứ không đi flat trên bản tin lượt.
            // Vào kho TRƯỚC khi enqueue: decor đọc bundle lúc dựng bản tin, vào sau là chính log
            // Start bị thiếu nhãn.
            _turnService.SetLevelConfig(param.currentLevel, movesLimit, timeLimitSec);
            _logScheduleService.Enqueue(new FLevelLog(param));
        }

        /// <summary>
        /// Nhịp giữa ván (heartbeat) — bản tin bảo hiểm kill-cứng của lượt chơi. Định danh lượt
        /// param không khai thì decor tự điền từ cache.
        /// </summary>
        public void LogLevelHeartbeat(LevelHeartBeatParamV2 param)
        {
            if (param == null) return;
            _logScheduleService.Enqueue(new FLevelLog(param));
        }

        /// <summary>Gửi log Pass — định danh lượt tự điền từ cache.</summary>
        public void LogLevelPass(LevelPassParamV2 param)
        {
            if (param == null) return;
            _logScheduleService.Enqueue(new FLevelLog(param));
        }

        /// <summary>Dựng LevelFailParamV2 và gửi log Fail — định danh lượt tự điền từ cache.</summary>
        public void LogLevelFail(LevelFailParamV2 param)
        {
            if (param == null) return;
            _logScheduleService.Enqueue(new FLevelLog(param));
        }
    }
}
