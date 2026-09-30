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
    public class AdLogService : MySingleton<AdLogService>, ILogDecorator<FAdLog>
    {
        private readonly IFPlayerAdRepository _adRepository;

        // Inject CACHE chứ không phải AdRequestService: decorator chỉ cần vai giữ-state, mà
        // service thì ôm LogScheduleService (vai bắn log) — inject cả cục là vòng chết DI
        // (án 12/08: Schedule→Decor→đây→AdRequestService→Schedule). Cache zero đường tới
        // LogScheduleService nên ctor-inject hợp lệ — luật ở ILogDecorator, test cấu trúc canh.
        private readonly AdViewCache _adViewCache;

        public AdLogService(IFPlayerAdRepository adRepository, AdViewCache adViewCache)
        {
            _adRepository = adRepository;
            _adViewCache = adViewCache;
        }

        public void Decor(FAdLog adLog)
        {
            var logParams = adLog.param;
            var (typeWatched, adLtv) = _adRepository.NewAdWatched(logParams.type, logParams.adRev);
            adLog.typeCount = typeWatched;
            adLog.adLtv = adLtv;

            // Bản tin gộp nhiều impression: mọi thứ dưới đây là của MỘT lần xem, mà "lần xem hiện
            // tại" lúc gửi chỉ là cái cuối cùng trong cụm — dán lên cả cụm là bịa cho N-1 cái còn
            // lại. Doanh thu ở trên vẫn cộng bình thường vì nó cộng được.
            if (adLog.IsSpanRecord) return;

            // Nối vào phễu fill: đóng request đang chờ của cùng format (§C). Mốc TIÊU THỤ —
            // impression thứ hai trên cùng view là SDK tự xoay id mới thay vì im lặng dùng lại
            // (án dup 5% loader 03/09: một adViewId bị tính tiền N lần vì mediation quên
            // OnRequested đợt mới; chốt owner: khuôn play_turn_id, tự sinh chứ không để trống).
            var (adViewId, fillLatencyMs, rotated) = _adViewCache.StampImpression(logParams.type);
            adLog.adViewId ??= adViewId;
            adLog.fillLatencyMs ??= fillLatencyMs;
            if (rotated)
                AnalyticLogger.Instance.Warning(
                    $"OnImpression({logParams.type}): view hiện tại đã có impression rồi — SDK xoay " +
                    "adViewId mới để không trùng mã. Đây là dấu hiệu mediation thiếu OnRequested " +
                    "cho đợt load mới (mỗi đợt xin ad là một request).");

            // Chỗ chiếu / ngữ cảnh / mediation: cùng luật hai chiều với mốc show và close
            // (mốc tự nhập thì thắng và cập nhật cache; bỏ trống thì lấy từ cache) — luật nằm MỘT
            // chỗ trong AdViewState.ApplyContext, đây chỉ là mốc thứ ba gọi vào nó.
            _adViewCache.ApplyContext(logParams, logParams.type);
        }
    }
}
