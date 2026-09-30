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
    public class SessionLogService : MySingleton<SessionLogService>, ILogDecorator<FSessionLog>
    {
        private readonly IFPlayerSessionRepository _sessionService;

        public SessionLogService(IFPlayerSessionRepository sessionService)
        {
            _sessionService = sessionService;
        }

        public void Decor(FSessionLog log)
        {
            // USER_TOTAL_TIME do PlayerSessionService (Devkit) làm chủ và đã cộng lúc app pause.
            // Cộng thêm ở đây là DOUBLE-WRITE cùng một khoảng → total_play_time chạy 2× tốc độ thật
            // (án 2026-08-05: loader probe fleet thấy cụm game ratio 1.31–1.84). Ở đây chỉ ĐỌC tổng.
            // gameMode riêng của game thì đây là writer duy nhất → vẫn cộng bình thường.
            var seconds = log.param.gameMode == PlayerSessionService.TOTAL_TIME_MODE
                ? 0
                : (long)log.param.sessionTime.TotalSeconds;

            log.modeTotalTime = _sessionService.IncreaseModeTotalSec(log.param.gameMode, seconds);
        }
    }
}