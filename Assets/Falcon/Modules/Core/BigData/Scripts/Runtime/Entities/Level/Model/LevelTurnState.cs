/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-30
 */

using System;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bộ tham số bất biến của một lượt chơi đang dở — game persist object này vào save
    /// (serializable, JSON-friendly) khi người chơi rời ván, trả lại cho Level.OnResume khi mở lại.
    /// <br/>Không có <c>movesLimit</c>/<c>timeLimitSec</c>: chúng tả MÀN chứ không tả lượt, nên
    /// nằm trong kho nhãn màn — vốn tự sống qua restart, khỏi nhờ save của game vác hộ.
    /// </summary>
    [Serializable]
    public class LevelTurnSnapshot
    {
        public string playTurnId;
        public int currentLevel;
        public string currentLevelId;
        public string difficulty;
    }

    /// <summary>
    /// State machine turn cho entity level (xem EntityLifecycle-Design.md):
    /// <br/>Start ── OPEN ──► terminal (Pass/Fail) ── ENDED ──► Start kế tiếp; rời ván dở đi
    /// nhánh SUSPENDED (log trễ của ván đó vẫn ăn cache).
    /// <br/>- Biên turn: <b>TERMINAL ĐÓNG LƯỢT</b> (chốt game-theory 04/09: game chỉ log fail khi
    ///   hết đường lật kèo — không tồn tại ca revive-cùng-lượt, nên 2 terminal = 2 lượt chơi
    ///   khác nhau). Terminal đến khi lượt đã ENDED: khác level / khác kết quả → seed id MỚI +
    ///   anomaly — <b>quên Start tự lành MỌI lần</b>, không riêng lần đầu (án cũ: từ ván mồ côi
    ///   thứ hai, id ván trước bị dùng lại và argMax phía server có thể lật ngược kết quả ván
    ///   trước); CÙNG kết quả và không mâu thuẫn định danh → nghi double-fire, giữ id cũ (server
    ///   argMax khử trùng như cũ, guard streak theo (turnId, status) không đếm đôi). Vùng mù
    ///   chấp nhận: quên Start + gọi scalar + cùng level cùng kết quả — hẹp, và có warning.
    /// <br/>- Heartbeat mù định danh sau ENDED = tick trễ của ván vừa đóng — giữ id, không mở
    ///   lại turn (mở là stamp cross-entity bật cho một ván ma).
    /// <br/>- <see cref="OpenPlayTurnId"/> chỉ khác null ở pha OPEN — dùng để stamp cross-entity
    ///   (ad/iap/resource...); sau terminal/suspend thôi stamp (§C: không bịa last_turn_id).
    /// <br/>- Pure class (không IO/DI/time) để test được; caller tự lo thread-safety.
    /// </summary>
    public class LevelTurnState
    {
        public enum Phase
        {
            None, Open, Ended,
            /// <summary>Rời ván dở (save-and-quit) — cache vẫn phục vụ log trễ của ván, khác ENDED (ván đã kết luận).</summary>
            Suspended
        }

        public enum Anomaly
        {
            None,
            /// <summary>Start đến khi turn trước còn mở — Start mới đè (luật A3 lồng nhau).</summary>
            StartWhileOpen,
            /// <summary>Terminal/heartbeat đến khi không có lượt nào nhận nó — sinh id tại chỗ (server nhận diện "cụt đầu").</summary>
            EventWithoutStart,
            /// <summary>Restore đến khi đang có turn mở — restore đè (kiểm tra lại luồng resume của game).</summary>
            RestoreWhileOpen,
            /// <summary>Terminal lặp lại cùng kết quả sau khi lượt đã đóng — nghi double-fire, giữ id cũ (nếu là ván chơi lại thật thì game đang thiếu OnStart).</summary>
            DuplicateTerminal
        }

        private string _playTurnId;
        private int _currentLevel;
        private string _currentLevelId;
        private string _difficulty;
        private LevelStatus? _lastTerminalStatus;

        public Phase CurrentPhase { get; private set; } = Phase.None;

        /// <summary>Id để stamp lên log của entity KHÁC — chỉ khác null ở pha OPEN.</summary>
        public string OpenPlayTurnId => CurrentPhase == Phase.Open ? _playTurnId : null;

        /// <summary>
        /// Áp một level log vào state machine: Start seed cache + mở turn; các status khác được
        /// điền field "chưa nhập" (giá trị default) từ cache — giá trị user nhập luôn thắng.
        /// </summary>
        public Anomaly Apply(ALevelParamV2 param)
        {
            return param.Status == LevelStatus.Start ? ApplyStart(param) : ApplyFollowUp(param);
        }

        /// <summary>
        /// Người chơi RỜI ván dở (thoát ra menu / save-and-quit): trả về snapshot bộ tham số cache
        /// để game persist, đồng thời chuyển turn sang SUSPENDED — tắt stamp cross-entity (ad ở
        /// menu không mang playTurnId nữa) nhưng cache vẫn phục vụ level log trễ của ván đó
        /// (khác ENDED: ván suspended chưa kết luận, terminal tới sau vẫn thuộc về nó).
        /// Trả về null nếu không có turn đang mở.
        /// </summary>
        public LevelTurnSnapshot Suspend()
        {
            var snapshot = TakeSnapshot();
            if (snapshot != null) CurrentPhase = Phase.Suspended;
            return snapshot;
        }

        /// <summary>
        /// Đọc THUẦN bộ tham số của ván đang chơi — không đổi pha, không tắt stamp cross-entity.
        /// Dùng cho autosave giữa ván: game muốn cất state vào save mà người chơi vẫn đang chơi
        /// tiếp (<see cref="Suspend"/> mang nghĩa "rời ván" nên không dùng cho việc này được).
        /// <br/>Null nếu không có ván nào đang mở — kể cả khi vừa có turn kết thúc: ván đã xong
        /// thì không còn gì để mở lại, trả snapshot ra chỉ mời gọi resume nhầm một lượt đã đóng.
        /// </summary>
        public LevelTurnSnapshot TakeSnapshot()
        {
            if (CurrentPhase != Phase.Open) return null;
            return new LevelTurnSnapshot
            {
                playTurnId = _playTurnId,
                currentLevel = _currentLevel,
                currentLevelId = _currentLevelId,
                difficulty = _difficulty
            };
        }

        /// <summary>
        /// Cập nhật tham số cấu hình của ván ĐANG CHƠI (revive đổi độ khó...): đối số khác null
        /// mới ghi đè.
        /// <br/>Cố tình KHÔNG cho sửa <c>playTurnId</c> (SDK sinh) và <c>currentLevel</c>/
        /// <c>currentLevelId</c> — đó là DANH TÍNH của lượt, đổi giữa chừng nghĩa là lượt khác,
        /// mà lượt khác thì phải mở turn mới chứ không phải sửa turn cũ.
        /// </summary>
        /// <returns>false nếu không có ván nào đang mở (không có gì để cập nhật).</returns>
        public bool Update(string difficulty)
        {
            if (CurrentPhase != Phase.Open) return false;
            if (!string.IsNullOrEmpty(difficulty)) _difficulty = difficulty;
            return true;
        }

        /// <summary>
        /// Mở lại turn của ván dở đã save (re-open qua restart app). KHÔNG phát event nào —
        /// chỉ set lại state để các log sau (heartbeat/terminal) mang đúng id + bundle của ván cũ.
        /// Game tự persist bundle trong save của họ (SDK không persist — luật A3.4 giữ nguyên).
        /// </summary>
        public Anomaly Restore(
            string playTurnId, int currentLevel, string currentLevelId, string difficulty)
        {
            var anomaly = CurrentPhase == Phase.Open ? Anomaly.RestoreWhileOpen : Anomaly.None;
            _playTurnId = playTurnId;
            _currentLevel = currentLevel;
            _currentLevelId = currentLevelId;
            _difficulty = difficulty;
            CurrentPhase = Phase.Open;
            return anomaly;
        }

        private Anomaly ApplyStart(ALevelParamV2 param)
        {
            var anomaly = CurrentPhase == Phase.Open ? Anomaly.StartWhileOpen : Anomaly.None;
            SeedFrom(param);
            CurrentPhase = Phase.Open;
            return anomaly;
        }

        private Anomaly ApplyFollowUp(ALevelParamV2 param)
        {
            var isTerminal = param.Status != LevelStatus.HeartBeat;

            // OPEN / SUSPENDED: follow-up thuộc lượt trong cache — điền thiếu, terminal đóng lượt.
            // (SUSPENDED là luồng thiết kế: rời ván dở rồi log trễ vẫn ăn id ván đó — xem Suspend.)
            if (CurrentPhase is Phase.Open or Phase.Suspended)
            {
                FillMissing(param);
                if (isTerminal) EndTurn(param.Status);
                return Anomaly.None;
            }

            if (CurrentPhase == Phase.Ended && !ConflictsWithCache(param))
            {
                // Heartbeat mù định danh = tick trễ của ván vừa đóng: giữ id, KHÔNG mở lại turn
                // (mở là bật stamp cross-entity cho một ván ma).
                if (!isTerminal)
                {
                    FillMissing(param);
                    return Anomaly.None;
                }

                // Terminal CÙNG kết quả, không mâu thuẫn định danh: nghi double-fire — giữ id để
                // server argMax khử trùng như cũ và guard streak (khoá turnId+status) không đếm
                // đôi. Nếu là ván chơi lại thật thì game thiếu OnStart — warning nhắc.
                if (param.Status == _lastTerminalStatus)
                {
                    FillMissing(param);
                    return Anomaly.DuplicateTerminal;
                }
            }

            // NONE, hoặc ENDED với bằng chứng lượt MỚI (khác level / kết quả khác lượt trước):
            // 2 terminal = 2 lượt chơi (chốt game-theory 04/09) — seed id mới tại chỗ, server
            // nhận diện "cụt đầu" qua anomaly + vắng dòng Start. Quên Start tự lành MỌI lần.
            SeedFrom(param);
            CurrentPhase = Phase.Open;
            if (isTerminal) EndTurn(param.Status);
            return Anomaly.EventWithoutStart;
        }

        private void EndTurn(LevelStatus status)
        {
            CurrentPhase = Phase.Ended;
            _lastTerminalStatus = status;
        }

        /// <summary>
        /// Param mang định danh MÂU THUẪN với lượt trong cache — bằng chứng chắc chắn nó thuộc
        /// một lượt khác. Field vắng mặt (level 0 / id null) không phải bằng chứng gì.
        /// </summary>
        private bool ConflictsWithCache(ALevelParamV2 param)
        {
            if (param.currentLevel != 0 && param.currentLevel != _currentLevel) return true;
            return param.currentLevelId != null && param.currentLevelId != _currentLevelId;
        }

        private void SeedFrom(ALevelParamV2 param)
        {
            if (string.IsNullOrEmpty(param.playTurnId)) param.playTurnId = Guid.NewGuid().ToString();
            _playTurnId = param.playTurnId;
            _currentLevel = param.currentLevel;
            _currentLevelId = param.currentLevelId;
            _difficulty = param.difficulty;
        }

        private void FillMissing(ALevelParamV2 param)
        {
            if (string.IsNullOrEmpty(param.playTurnId)) param.playTurnId = _playTurnId;
            if (param.currentLevel == 0) param.currentLevel = _currentLevel;
            if (param.currentLevelId == null) param.currentLevelId = _currentLevelId;
            if ((string.IsNullOrEmpty(param.difficulty) || param.difficulty == FParam.UNKNOWN)
                && !string.IsNullOrEmpty(_difficulty))
                param.difficulty = _difficulty;
        }
    }
}
