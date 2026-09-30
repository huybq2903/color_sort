using System;
using System.Collections.Generic;
using Falcon.Modules.Core.BigData;
using Falcon.Modules.Level.Core;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    /// <summary>
    /// Khi app bị vuốt (interupt context) thì rất khó để gửi log lên server.
    /// Nếu để dev tự log bản tin độc lập thì sẽ sinh ra rất nhiều luồng gửi bản tin khi ngắt app, gây rất nhiều vấn đề.
    /// <br/>
    /// IAppPauseLogGenerator là interface để hệ thống thu thập các service cần log khi app ngắt, tạo batch và gửi lên 1 luồng duy nhất, khắc phục vấn đề trên.
    /// <br/>
    /// ALevelHeartBeatService là class để người dùng kế thừa, khai báo các tham số về level user đang chơi.
    /// Khi app bị vuốt, nếu ALevelHeartBeatService.IsPlayingLevel thì sẽ thu thập tham số và gửi 1 bản tin LevelHeartBeatParamV2 để tránh mất toàn bộ tham số khi người dùng drop level.
    /// <br/><br/>
    /// <b>Ranh giới bắt buộc/mặc định theo AI-BIẾT</b>: mọi thứ chỉ GAME biết đều abstract —
    /// <see cref="CurrentLevelPlayTime"/>, <see cref="CurrentLevelScore"/>,
    /// <see cref="CurrentLevelNumberMove"/>, <see cref="CurrentLevelTotalScore"/>,
    /// <see cref="CurrentLevelBoosterUsed"/>, <see cref="CurrentLevelExtraParams"/> — "không có"
    /// (0/null) phải là câu trả lời của game, SDK không điền hộ. Thứ duy nhất có mặc định là
    /// <see cref="IsPlayingLevel"/> vì SDK TỰ SUY được (playTurnId khác null). Định danh lượt chơi
    /// thì SDK đã cache từ lúc Start (playTurnId, level, levelId, difficulty, movesLimit,
    /// timeLimitSec — không phải khai lại); tham số 0/null sẽ vắng mặt khỏi payload chứ không gửi
    /// số giả (§H4).
    /// </summary>
    public abstract class ALevelHeartBeatService : IAppPauseLogGenerator
    {
        private readonly FLevelManager _levelManager;

        protected ALevelHeartBeatService(FLevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public IEnumerable<IDataLog> GetLogsOnAppPause()
        {
            if (!IsPlayingLevel) yield break;

            // Gọi mỗi property đúng một lần: bản cài của game có thể tốn kém hoặc có side effect
            var playTime = CurrentLevelPlayTime;
            var score = CurrentLevelScore;
            var totalScore = CurrentLevelTotalScore;
            var movesUsed = CurrentLevelNumberMove;

            var level = LevelData.Instance.level;
            var param = new LevelHeartBeatParamV2
            {
                // Bộ định danh dưới đây SDK cũng tự điền được từ cache của lượt; điền sẵn ở đây làm
                // lưới an toàn cho ca heartbeat đến mà chưa từng có log Start (vd app restart giữa
                // màn) — lúc đó cache rỗng và param sẽ trống trơn.
                playTurnId = _levelManager.PlayTurnId,
                currentLevel = level,
                currentLevelId = _levelManager.CurrentMd5LevelDataParam,
                difficulty = _levelManager.GetLevelDifficulty(level).ToString(),

                boostersUsed = CurrentLevelBoosterUsed,
                extraMeta = CurrentLevelExtraParams
            };

            // Chỉ điền cái đo được: game không theo dõi score/moves thì để field vắng mặt, đừng gửi
            // 0 — 0 trông như một giá trị hợp lệ nên không ai đi kiểm (§H4)
            if (playTime > TimeSpan.Zero) param.duration = playTime;
            if (score > 0) param.score = score;
            if (movesUsed > 0) param.movesUsed = movesUsed;

            // totalScore == 0 (game không có hệ điểm, hoặc màn chưa nạp xong) thì KHÔNG chia:
            // exception ở đây giết luôn cả batch log lúc app pause, không riêng bản tin này
            if (totalScore > 0) param.levelProgress = (int)Math.Min(100, score * 100L / totalScore);

            yield return new FLevelLog(param);
        }

        /// <summary>
        /// Có đang trong một lượt chơi không. Mặc định: SDK có lượt nào đang MỞ hay không
        /// (playTurnId khác null) — game nào có định nghĩa riêng thì override.
        /// </summary>
        public virtual bool IsPlayingLevel => _levelManager.PlayTurnId != null;

        public abstract TimeSpan CurrentLevelPlayTime { get; }

        public abstract int CurrentLevelScore { get; }

        public abstract int CurrentLevelNumberMove { get; }

        /// <summary>
        /// Tổng điểm của màn — chỉ dùng để quy ra <c>levelProgress</c>. Game không có hệ điểm thì
        /// trả 0: SDK sẽ không gửi levelProgress thay vì gửi một tỉ lệ bịa.
        /// <br/>ABSTRACT có chủ đích (đổi từ virtual mặc định 0 của 1.1.4): giá trị này chỉ game
        /// biết, SDK không cache được — mặc định là ĐIỀN HỘ, và quên override là levelProgress mất
        /// im lặng. Trả 0 phải là CÂU TRẢ LỜI của game, không phải cái SDK đoán.
        /// </summary>
        public abstract int CurrentLevelTotalScore { get; }

        /// <summary>
        /// Số booster đã sử dụng trong level của mỗi loại — không dùng booster / game không có
        /// booster thì trả null (field vắng mặt khỏi payload, §H4).
        /// <br/>ABSTRACT cùng lý do <see cref="CurrentLevelTotalScore"/>: chỉ game biết, null phải
        /// là câu trả lời chứ không phải mặc định.
        /// <remarks>buyMoreTime cũng tính là booster và nên nằm trong Dicitonary này </remarks>
        /// </summary>
        public abstract Dictionary<string, int> CurrentLevelBoosterUsed { get; }

        /// <summary>
        /// Các tham số thêm để phân tích level — không có thì trả null. Ít nhất nên gồm:
        /// <list type="bullet">
        ///     <item>
        ///         <term>coinSpend :</term>
        ///         <description>số vàng đã sử dụng để chơi level(dùng để mua booster/revive/...)</description>
        ///     </item>
        /// </list>
        /// ABSTRACT cùng lý do <see cref="CurrentLevelTotalScore"/>.
        /// </summary>
        public abstract Dictionary<string, object> CurrentLevelExtraParams { get; }
    }
}
