/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-07
 */

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Entity LƯỢT CHƠI — <c>FalconBigDataController.Level</c> (xem EntityLifecycle-Design.md §2).
    /// <br/>Tham số định danh của lượt (playTurnId, currentLevel, currentLevelId, difficulty,
    /// movesLimit, timeLimitSec) được cache từ <see cref="OnStart"/>, các khoảnh khắc sau KHÔNG
    /// phải nhập lại — chỉ nhập phần mới phát sinh.
    /// </summary>
    public class FLevelApi
    {
        private readonly LevelLogService _logService;
        private readonly LevelTurnService _turnService;
        private readonly LevelLogDecorService _decorService;
        private readonly LabelLogService _labelLogService;

        internal FLevelApi(
            LevelLogService logService, LevelTurnService turnService, LevelLogDecorService decorService,
            LabelLogService labelLogService)
        {
            _logService = logService;
            _turnService = turnService;
            _decorService = decorService;
            _labelLogService = labelLogService;
        }

        // ---- Đọc / sửa cache của lượt ----

        /// <summary>playTurnId của lượt chơi ĐANG MỞ (null nếu không có).</summary>
        public string CurrentTurnId => _turnService.OpenPlayTurnId;

        /// <summary>
        /// Bộ tham số của ván ĐANG CHƠI, đọc thuần — không đóng ván, không tắt stamp playTurnId.
        /// Dùng cho autosave giữa ván (<see cref="OnPauseExit"/> mang nghĩa "rời ván" nên không
        /// dùng cho việc này được). Null nếu không có ván nào đang mở.
        /// </summary>
        public LevelTurnSnapshot CurrentTurn => _turnService.CurrentSnapshot();

        /// <summary>
        /// Chuỗi THẮNG hiện tại do SDK tự tính (xuyên level, xuyên session). Đọc ở đây thay vì
        /// game tự đếm lần hai — đếm hai nơi thì sớm muộn UI và log lệch nhau mà không ai biết
        /// bên nào đúng. Hết hạn chuỗi theo luật của game thì gọi <see cref="ResetWinStreak"/>.
        /// </summary>
        public int WinStreak => _decorService.WinStreak;

        /// <inheritdoc cref="WinStreak"/>
        public int LoseStreak => _decorService.LoseStreak;

        /// <summary>
        /// Cập nhật tham số CẤU HÌNH của ván đang chơi khi nó đổi giữa chừng: mua thêm lượt đi,
        /// revive đổi độ khó... Chỉ đối số khác null mới ghi đè.
        /// <br/>⚠ <paramref name="movesLimit"/>/<paramref name="timeLimitSec"/> là config của BẢN
        /// THIẾT KẾ MÀN (hợp đồng xếp chúng chủ thể LEVEL), nên sửa ở đây là sửa cho MỌI lần chơi
        /// màn đó về sau, không riêng ván này. Số lượt CÒN LẠI của ván thì đừng gửi qua đây.
        /// <br/>Trước đây muốn đổi phải gọi lại <see cref="OnStart"/> — mà thế là ĐÈ TURN, sai
        /// vòng đời (một ván thành hai lượt trong dữ liệu).
        /// <br/>Cố tình không cho đổi <c>currentLevel</c>/<c>currentLevelId</c>: đó là danh tính
        /// của lượt, đổi giữa chừng nghĩa là lượt khác — mà lượt khác thì mở turn mới.
        /// </summary>
        public void UpdateTurn(string difficulty = null, int? movesLimit = null, int? timeLimitSec = null)
        {
            _turnService.UpdateTurn(difficulty, movesLimit, timeLimitSec);
        }

        /// <summary>
        /// Khai config của BẢN THIẾT KẾ MÀN (giới hạn lượt đi / thời gian) — nhãn của MÀN, đúng cho
        /// mọi lần chơi màn đó, nên khai một lần là đủ và SDK tự lưu qua restart.
        /// <br/>Bình thường KHÔNG cần gọi: <see cref="OnStart"/> đã nhận hai số này. Hàm này dành
        /// cho đường tự dựng <c>FLevelLog</c> (vd FLevelManager) hoặc lúc game biết config trước khi
        /// vào màn. Đối số null = không nhắc tới, KHÔNG phải gỡ.
        /// </summary>
        public void SetLevelConfig(int currentLevel, int? movesLimit = null, int? timeLimitSec = null)
        {
            _turnService.SetLevelConfig(currentLevel, movesLimit, timeLimitSec);
        }

        /// <summary>
        /// Khai điểm trình độ NGƯỜI CHƠI — gọi lại mỗi khi nó đổi; gọi lại cùng giá trị thì SDK tự
        /// bỏ qua nên không sợ tốn bản tin.
        /// <code>
        /// FalconBigDataController.Level.SetUserElo(PlayerRating.Current);
        /// </code>
        /// Trước 1.3.0 đây là provider và elo được đóng phẳng lên MỌI level event. Sai chủ thể
        /// (§H6: elo tả người chơi, không tả lượt) và nhân bản một sự thật của user lên stream dày
        /// nhất hệ; nay nó là nhãn user, gửi khi ĐỔI, server forward-fill lên các event sau.
        /// <br/>Chỉ game biết con số này nên game phải cấp. Ngược lại, ĐỘ KHÓ CỦA MÀN thì đừng gửi:
        /// nó là hàm của kết quả cả fleet, server tự giải ra từ tỉ lệ pass + elo người chơi;
        /// client gửi lên chỉ là đóng băng một giá trị cũ hơn bản server tự tính.
        /// <br/>Truyền null để GỠ nhãn.
        /// </summary>
        public void SetUserElo(int? elo)
        {
            _labelLogService.LabelUser(FUserLabelKey.ELO, elo);
        }

        /// <summary>
        /// Chuỗi thắng của game có luật hết hạn (vd N ngày không chơi là mất chuỗi) → gọi hàm này
        /// khi luật đó kích hoạt để winStreak trên log về 0. Chỉ reset — không có setter tùy ý
        /// (streak do SDK tự tính, set tay là claim không bằng chứng).
        /// </summary>
        public void ResetWinStreak()
        {
            _decorService.ResetWinStreak();
        }

        /// <inheritdoc cref="ResetWinStreak"/>
        public void ResetLoseStreak()
        {
            _decorService.ResetLoseStreak();
        }

        /// <summary>
        /// Người chơi RỜI ván dở (thoát ra menu / save-and-quit): trả về snapshot bộ tham số của
        /// lượt chơi để game persist vào save (serializable), đồng thời tắt stamp playTurnId cho
        /// các log ngoài gameplay (ad ở menu không mang id của lượt nữa).
        /// Trả về null nếu không có lượt nào đang mở.
        /// KHÔNG phát event nào — muốn tính là bỏ ván hẳn thì gọi <see cref="OnFail"/> với
        /// <see cref="LevelFailReason.Quit"/>.
        /// </summary>
        public LevelTurnSnapshot OnPauseExit()
        {
            return _turnService.SuspendTurn();
        }

        /// <summary>
        /// Người chơi mở lại VÁN DỞ (cùng phiên hoặc sau restart app): trả lại snapshot đã lấy từ
        /// <see cref="OnPauseExit"/> — heartbeat/terminal sau đó mang đúng playTurnId + bundle
        /// của ván cũ (1 ván = 1 turn dù qua restart). KHÔNG phát event nào.
        /// Snapshot null/mất id thì bỏ qua — cứ <see cref="OnStart"/> lượt mới, chấp nhận vỡ theo
        /// hợp đồng A3.4.
        /// </summary>
        public void OnResume(LevelTurnSnapshot snapshot)
        {
            if (snapshot == null) return;
            // Không phải vác movesLimit/timeLimitSec qua save nữa: chúng là nhãn của MÀN, SDK tự
            // lưu và tự nạp lại.
            OnResume(snapshot.playTurnId, snapshot.currentLevel, snapshot.difficulty,
                snapshot.currentLevelId);
        }

        /// <inheritdoc cref="OnResume(LevelTurnSnapshot)"/>
        public void OnResume(
            string playTurnId, int currentLevel, string difficulty = null, string currentLevelId = null,
            int? movesLimit = null, int? timeLimitSec = null)
        {
            _turnService.RestoreTurn(playTurnId, currentLevel, currentLevelId, difficulty,
                movesLimit, timeLimitSec);
        }

        // ---- Báo khoảnh khắc (phát event) ----

        /// <summary>
        /// Người chơi bắt đầu một lượt chơi level. Mở turn mới (đè turn cũ nếu còn mở) —
        /// từ đây mọi log (ad/iap/resource...) tự mang playTurnId cho tới khi lượt kết thúc.
        /// </summary>
        /// <param name="currentLevel">Số level đang chơi (bắt buộc — seed cho cache của cả lượt).</param>
        /// <param name="difficulty">Độ khó của màn.</param>
        /// <param name="currentLevelId">Danh tính BẢN THIẾT KẾ level (đổi tune/AB = id mới).</param>
        /// <param name="movesLimit">Giới hạn lượt đi của màn (config màn). Không biết thì bỏ trống, đừng điền bừa.</param>
        /// <param name="timeLimitSec">Giới hạn thời gian màn (giây). Không biết thì bỏ trống, đừng điền bừa.</param>
        /// <param name="param">Tham số của LƯỢT — <c>param.currentLevel</c> bắt buộc.</param>
        /// <param name="movesLimit">
        /// Giới hạn lượt đi của màn. CỐ Ý nằm ngoài param: đây là config của BẢN THIẾT KẾ MÀN
        /// (§H6 — chủ thể LEVEL), SDK đưa vào kho nhãn chứ không đi flat trên bản tin lượt.
        /// </param>
        /// <param name="timeLimitSec">Giới hạn thời gian màn (giây) — cùng lý do nằm ngoài param.</param>
        public void OnStart(LevelStartParamV2 param, int? movesLimit = null, int? timeLimitSec = null)
        {
            _logService.LogLevelStart(param, movesLimit, timeLimitSec);
        }

        /// <inheritdoc cref="OnStart(LevelStartParamV2,int?,int?)"/>
        /// <param name="currentLevel">Số level đang chơi (bắt buộc — seed cho cache của cả lượt).</param>
        /// <param name="difficulty">Độ khó của màn.</param>
        /// <param name="currentLevelId">Danh tính BẢN THIẾT KẾ level (đổi tune/AB = id mới).</param>
        /// <param name="preBoostersUsed">Booster chọn trước khi vào màn.</param>
        /// <param name="extraMeta">Tham số game-specific CHƯA có trong hợp đồng (được lưu event_extra_props, xin vào hợp đồng sau).</param>
        public void OnStart(
            int currentLevel, string difficulty = null, string currentLevelId = null,
            int? movesLimit = null, int? timeLimitSec = null,
            Dictionary<string, int> preBoostersUsed = null, Dictionary<string, object> extraMeta = null)
        {
            var param = new LevelStartParamV2
            {
                currentLevel = currentLevel,
                currentLevelId = currentLevelId,
                preBoostersUsed = preBoostersUsed,
                extraMeta = extraMeta
            };
            if (difficulty != null) param.difficulty = difficulty;
            _logService.LogLevelStart(param, movesLimit, timeLimitSec);
        }

        /// <summary>
        /// Nhịp giữa ván (heartbeat) — bản tin bảo hiểm cho lượt chơi dài, app bị kill cứng vẫn
        /// còn dấu vết tiến độ. Thường do heartbeat service của module gọi, game tự nối cũng được.
        /// </summary>
        public void OnHeartbeat(LevelHeartBeatParamV2 param)
        {
            _logService.LogLevelHeartbeat(param);
        }

        /// <summary>
        /// Người chơi thắng lượt hiện tại. Định danh lượt tự lấy từ cache (từ <see cref="OnStart(LevelStartParamV2,int?,int?)"/>).
        /// Sau khoảnh khắc này các log khác (ad chuyển màn...) thôi mang playTurnId.
        /// </summary>
        public void OnPass(LevelPassParamV2 param)
        {
            _logService.LogLevelPass(param);
        }

        /// <inheritdoc cref="OnPass(LevelPassParamV2)"/>
        public void OnPass(
            int? score = null, TimeSpan duration = default,
            Dictionary<string, int> boostersUsed = null, int? movesUsed = null,
            Dictionary<string, object> extraMeta = null)
        {
            _logService.LogLevelPass(new LevelPassParamV2
            {
                score = score,
                duration = duration,
                boostersUsed = boostersUsed,
                movesUsed = movesUsed,
                extraMeta = extraMeta
            });
        }

        /// <summary>
        /// Người chơi thua lượt hiện tại. Định danh lượt tự lấy từ cache (từ <see cref="OnStart(LevelStartParamV2,int?,int?)"/>).
        /// </summary>
        public void OnFail(LevelFailParamV2 param)
        {
            _logService.LogLevelFail(param);
        }

        /// <inheritdoc cref="OnFail(LevelFailParamV2)"/>
        /// <param name="failReason">Thua VÌ SAO (hợp đồng §D). Không biết thì bỏ trống, đừng điền bừa.</param>
        /// <param name="levelProgress">Đi được bao nhiêu % màn lúc thua (0-100).</param>
        public void OnFail(
            LevelFailReason? failReason = null, int? levelProgress = null,
            int? score = null, TimeSpan duration = default,
            Dictionary<string, int> boostersUsed = null, int? movesUsed = null,
            Dictionary<string, object> extraMeta = null)
        {
            var param = new LevelFailParamV2
            {
                failReason = failReason,
                score = score,
                duration = duration,
                boostersUsed = boostersUsed,
                movesUsed = movesUsed,
                extraMeta = extraMeta
            };
            if (levelProgress.HasValue) param.levelProgress = levelProgress.Value;
            _logService.LogLevelFail(param);
        }
    }
}
