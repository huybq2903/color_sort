using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class LevelTurnStateTests
    {
        // movesLimit/timeLimitSec không còn nằm trên param — chúng là nhãn của MÀN
        // (EntityLabelService), xem LevelParamV2Tests
        private static LevelStartParamV2 StartParam(int level = 5, string difficulty = "Hard",
            string levelId = "abc12")
        {
            var param = new LevelStartParamV2
            {
                currentLevel = level,
                currentLevelId = levelId
            };
            if (difficulty != null) param.difficulty = difficulty;
            return param;
        }

        [Test]
        public void Start_GeneratesId_AndWritesBackToParam()
        {
            var state = new LevelTurnState();
            var start = StartParam();
            var anomaly = state.Apply(start);

            Assert.AreEqual(LevelTurnState.Anomaly.None, anomaly);
            Assert.IsNotNull(start.playTurnId);
            Assert.AreEqual(start.playTurnId, state.OpenPlayTurnId);
            Assert.AreEqual(LevelTurnState.Phase.Open, state.CurrentPhase);
        }

        [Test]
        public void Start_KeepsExplicitId()
        {
            var state = new LevelTurnState();
            var start = StartParam();
            start.playTurnId = "my-id";
            state.Apply(start);
            Assert.AreEqual("my-id", state.OpenPlayTurnId);
        }

        [Test]
        public void FollowUp_FillsMissingIdentityFromCache()
        {
            var state = new LevelTurnState();
            var start = StartParam(level: 5, difficulty: "Hard", levelId: "abc12");
            state.Apply(start);

            var pass = new LevelPassParamV2(); // dev không nhập gì về định danh
            state.Apply(pass);

            Assert.AreEqual(start.playTurnId, pass.playTurnId);
            Assert.AreEqual(5, pass.currentLevel);
            Assert.AreEqual("abc12", pass.currentLevelId);
            Assert.AreEqual("Hard", pass.difficulty);
        }

        [Test]
        public void FollowUp_ExplicitValueWinsOverCache()
        {
            var state = new LevelTurnState();
            state.Apply(StartParam(level: 5, difficulty: "Hard"));

            var heartBeat = new LevelHeartBeatParamV2 { currentLevel = 7, difficulty = "Expert" };
            state.Apply(heartBeat);

            Assert.AreEqual(7, heartBeat.currentLevel);
            Assert.AreEqual("Expert", heartBeat.difficulty);
        }

        [Test]
        public void Terminal_MovesToEnded_CrossEntityStampingOff()
        {
            var state = new LevelTurnState();
            state.Apply(StartParam());
            Assert.IsNotNull(state.OpenPlayTurnId);

            state.Apply(new LevelPassParamV2());
            Assert.AreEqual(LevelTurnState.Phase.Ended, state.CurrentPhase);
            Assert.IsNull(state.OpenPlayTurnId, "Sau terminal, ad/iap/... không được stamp playTurnId nữa (§C)");
        }

        [Test]
        public void FailThenPass_IsTwoPlays_NewIdWithAnomaly()
        {
            // Chốt game-theory 04/09: game chỉ log fail khi hết đường lật kèo — Fail rồi Pass
            // nghĩa là CHƠI LẠI và thắng, tức 2 lượt khác nhau. Pass thiếu OnStart → id mới
            // sinh tại chỗ + anomaly (không dùng lại id của lượt đã thua).
            var state = new LevelTurnState();
            var start = StartParam();
            state.Apply(start);

            var fail = new LevelFailParamV2();
            state.Apply(fail);
            var pass = new LevelPassParamV2();
            var anomaly = state.Apply(pass);

            Assert.AreEqual(start.playTurnId, fail.playTurnId);
            Assert.AreNotEqual(start.playTurnId, pass.playTurnId);
            Assert.AreEqual(LevelTurnState.Anomaly.EventWithoutStart, anomaly);
        }

        [Test]
        public void DoubleFireTerminal_SameStatus_KeepsIdWithWarningAnomaly()
        {
            // Double-fire callback: cùng kết quả, không mâu thuẫn định danh → giữ id cũ để
            // server argMax khử trùng và guard streak (khoá turnId+status) không đếm đôi
            var state = new LevelTurnState();
            var start = StartParam();
            state.Apply(start);
            state.Apply(new LevelPassParamV2());

            var dup = new LevelPassParamV2();
            var anomaly = state.Apply(dup);

            Assert.AreEqual(start.playTurnId, dup.playTurnId);
            Assert.AreEqual(LevelTurnState.Anomaly.DuplicateTerminal, anomaly);
        }

        [Test]
        public void ForgottenStart_SecondOrphan_GetsFreshId()
        {
            // Án gốc: dev quên OnStart cho một mode — ván mồ côi THỨ HAI trước đây im lặng dùng
            // lại id ván trước (và argMax phía server có thể lật kết quả ván trước). Giờ: khác
            // level = bằng chứng lượt mới → id mới + anomaly, quên Start tự lành MỌI lần.
            var state = new LevelTurnState();
            var fail1 = new LevelFailParamV2 { currentLevel = 5 };
            state.Apply(fail1);
            var fail2 = new LevelFailParamV2 { currentLevel = 6 };
            var anomaly = state.Apply(fail2);

            Assert.AreNotEqual(fail1.playTurnId, fail2.playTurnId);
            Assert.AreEqual(LevelTurnState.Anomaly.EventWithoutStart, anomaly);
            Assert.AreEqual(6, fail2.currentLevel, "Không được điền level của ván trước vào ván này");
        }

        [Test]
        public void ForgottenStart_DifferentOutcome_GetsFreshId_EvenWithoutIdentity()
        {
            // Quên Start + gọi scalar (không level): kết quả KHÁC lượt trước vẫn đủ bằng chứng
            // lượt mới (fail rồi pass không thể là double-fire)
            var state = new LevelTurnState();
            var fail = new LevelFailParamV2 { currentLevel = 5 };
            state.Apply(fail);
            var pass = new LevelPassParamV2();
            var anomaly = state.Apply(pass);

            Assert.AreNotEqual(fail.playTurnId, pass.playTurnId);
            Assert.AreEqual(LevelTurnState.Anomaly.EventWithoutStart, anomaly);
        }

        [Test]
        public void SuspendedTurn_LateTerminal_StillBelongsToIt()
        {
            // Rời ván dở rồi game log fail trễ (quit flow): vẫn là ván ĐÓ — SUSPENDED khác ENDED
            var state = new LevelTurnState();
            var start = StartParam(level: 5);
            state.Apply(start);
            state.Suspend();

            var fail = new LevelFailParamV2();
            var anomaly = state.Apply(fail);

            Assert.AreEqual(start.playTurnId, fail.playTurnId);
            Assert.AreEqual(LevelTurnState.Anomaly.None, anomaly);
            Assert.AreEqual(LevelTurnState.Phase.Ended, state.CurrentPhase);
        }

        [Test]
        public void HeartbeatAfterTerminal_StillFilledFromCache()
        {
            var state = new LevelTurnState();
            var start = StartParam(level: 5);
            state.Apply(start);
            state.Apply(new LevelPassParamV2());

            var lateHeartBeat = new LevelHeartBeatParamV2();
            state.Apply(lateHeartBeat);
            Assert.AreEqual(start.playTurnId, lateHeartBeat.playTurnId);
            Assert.AreEqual(5, lateHeartBeat.currentLevel);
            Assert.AreEqual(LevelTurnState.Phase.Ended, state.CurrentPhase, "Heartbeat trễ không mở lại turn");
        }

        [Test]
        public void StartAfterEnded_OpensNewTurnWithNewId()
        {
            var state = new LevelTurnState();
            var start1 = StartParam(level: 5);
            state.Apply(start1);
            state.Apply(new LevelPassParamV2());

            var start2 = StartParam(level: 6, difficulty: null, levelId: null);
            var anomaly = state.Apply(start2);

            Assert.AreEqual(LevelTurnState.Anomaly.None, anomaly);
            Assert.AreNotEqual(start1.playTurnId, start2.playTurnId);
            Assert.AreEqual(start2.playTurnId, state.OpenPlayTurnId);

            // Cache là của turn MỚI: levelId của turn cũ không được rò sang
            var pass = new LevelPassParamV2();
            state.Apply(pass);
            Assert.AreEqual(6, pass.currentLevel);
            Assert.IsNull(pass.currentLevelId);
        }

        [Test]
        public void StartWhileOpen_Overrides_WithAnomaly()
        {
            var state = new LevelTurnState();
            var start1 = StartParam(level: 5);
            state.Apply(start1);

            var start2 = StartParam(level: 6);
            var anomaly = state.Apply(start2);

            Assert.AreEqual(LevelTurnState.Anomaly.StartWhileOpen, anomaly);
            Assert.AreEqual(start2.playTurnId, state.OpenPlayTurnId);
        }

        [Test]
        public void TerminalWithoutStart_GeneratesIdInPlace()
        {
            var state = new LevelTurnState();
            var fail = new LevelFailParamV2 { currentLevel = 3 };
            var anomaly = state.Apply(fail);

            Assert.AreEqual(LevelTurnState.Anomaly.EventWithoutStart, anomaly);
            Assert.IsNotNull(fail.playTurnId);
            Assert.AreEqual(LevelTurnState.Phase.Ended, state.CurrentPhase);
        }

        [Test]
        public void TakeSnapshot_DoesNotCloseTheTurn()
        {
            // Autosave giữa ván: cất state vào save mà người chơi vẫn đang chơi tiếp
            var state = new LevelTurnState();
            var start = StartParam(level: 7, levelId: "lv7");
            state.Apply(start);

            var snapshot = state.TakeSnapshot();

            Assert.AreEqual(start.playTurnId, snapshot.playTurnId);
            Assert.AreEqual(7, snapshot.currentLevel);
            Assert.AreEqual("lv7", snapshot.currentLevelId);
            Assert.AreEqual(start.playTurnId, state.OpenPlayTurnId, "Ván vẫn đang mở, ad trong màn vẫn mang id");
            Assert.AreEqual(LevelTurnState.Phase.Open, state.CurrentPhase);
        }

        [Test]
        public void TakeSnapshot_NoOpenTurn_ReturnsNull()
        {
            var state = new LevelTurnState();
            Assert.IsNull(state.TakeSnapshot());

            state.Apply(StartParam());
            state.Apply(new LevelPassParamV2());
            Assert.IsNull(state.TakeSnapshot(), "Ván đã xong thì không còn gì để mở lại");
        }

        [Test]
        public void Update_ChangesConfigMidTurn_AndFollowUpLogsCarryIt()
        {
            // Revive đổi độ khó giữa ván: trước đây phải OnLevelStart lại = đè turn = sai vòng đời
            var state = new LevelTurnState();
            var start = StartParam();
            state.Apply(start);

            Assert.IsTrue(state.Update("Expert"));

            var fail = new LevelFailParamV2();
            state.Apply(fail);
            Assert.AreEqual("Expert", fail.difficulty);
            Assert.AreEqual(start.playTurnId, fail.playTurnId, "Vẫn là cùng một lượt");
        }

        [Test]
        public void Update_KeepsIdentity_TurnStaysTheSame()
        {
            var state = new LevelTurnState();
            var start = StartParam(level: 5);
            state.Apply(start);

            state.Update("Easy");

            var pass = new LevelPassParamV2();
            state.Apply(pass);
            Assert.AreEqual(5, pass.currentLevel, "Update không đụng tới danh tính lượt");
            Assert.AreEqual(start.playTurnId, pass.playTurnId);
        }

        [Test]
        public void Update_NoOpenTurn_IsRejected()
        {
            // Sửa cache của một lượt không tồn tại chỉ làm bẩn lượt sau
            var state = new LevelTurnState();
            Assert.IsFalse(state.Update("Hard"));

            state.Apply(StartParam());
            state.Apply(new LevelPassParamV2());
            Assert.IsFalse(state.Update("Hard"), "Ván đã kết thúc thì không sửa được nữa");
        }

        [Test]
        public void Suspend_ReturnsSnapshot_AndStopsCrossEntityStamping()
        {
            var state = new LevelTurnState();
            var start = StartParam(level: 5, difficulty: "Hard", levelId: "abc12");
            state.Apply(start);

            var snapshot = state.Suspend();

            Assert.IsNotNull(snapshot);
            Assert.AreEqual(start.playTurnId, snapshot.playTurnId);
            Assert.AreEqual(5, snapshot.currentLevel);
            Assert.AreEqual("Hard", snapshot.difficulty);
            Assert.IsNull(state.OpenPlayTurnId, "Rời ván rồi thì ad ở menu không được mang playTurnId");
            Assert.AreEqual(LevelTurnState.Phase.Suspended, state.CurrentPhase,
                "Rời ván dở là SUSPENDED (log trễ còn ăn cache) — không phải ENDED (đã kết luận)");
        }

        [Test]
        public void Suspend_NoOpenTurn_ReturnsNull()
        {
            var state = new LevelTurnState();
            Assert.IsNull(state.Suspend());

            state.Apply(StartParam());
            state.Apply(new LevelPassParamV2());
            Assert.IsNull(state.Suspend(), "Turn đã terminal thì không còn gì để save");
        }

        [Test]
        public void SuspendThenRestore_RoundTrip_SameTurn()
        {
            // Luồng save-ván-dở: PauseExit → (persist/restart) → Resume → chơi tiếp cùng turn
            var state = new LevelTurnState();
            var start = StartParam(level: 5);
            state.Apply(start);
            var snapshot = state.Suspend();

            var restored = new LevelTurnState(); // mô phỏng process mới sau restart
            restored.Restore(snapshot.playTurnId, snapshot.currentLevel, snapshot.currentLevelId,
                snapshot.difficulty);

            var pass = new LevelPassParamV2();
            restored.Apply(pass);
            Assert.AreEqual(start.playTurnId, pass.playTurnId);
            Assert.AreEqual(5, pass.currentLevel);
            Assert.AreEqual("Hard", pass.difficulty);
        }

        [Test]
        public void Restore_ReopensTurnWithSavedId_FollowUpInherits()
        {
            // Ván dở save qua restart app: game restore lại bundle → 1 ván = 1 turn
            var state = new LevelTurnState();
            var anomaly = state.Restore("saved-id", 5, "abc12", "Hard");

            Assert.AreEqual(LevelTurnState.Anomaly.None, anomaly);
            Assert.AreEqual("saved-id", state.OpenPlayTurnId);

            var pass = new LevelPassParamV2();
            state.Apply(pass);
            Assert.AreEqual("saved-id", pass.playTurnId);
            Assert.AreEqual(5, pass.currentLevel);
            Assert.AreEqual("Hard", pass.difficulty);
            Assert.AreEqual(LevelTurnState.Phase.Ended, state.CurrentPhase);
        }

        [Test]
        public void Restore_WhileOpen_OverridesWithAnomaly()
        {
            var state = new LevelTurnState();
            state.Apply(StartParam(level: 5));

            var anomaly = state.Restore("saved-id", 3, null, null);
            Assert.AreEqual(LevelTurnState.Anomaly.RestoreWhileOpen, anomaly);
            Assert.AreEqual("saved-id", state.OpenPlayTurnId);
        }

        [Test]
        public void Restore_NullDifficultyInCache_DoesNotOverwriteParamDefault()
        {
            var state = new LevelTurnState();
            state.Restore("saved-id", 3, null, null);

            var fail = new LevelFailParamV2();
            state.Apply(fail);
            Assert.IsNotNull(fail.difficulty, "Cache thiếu difficulty thì không được ghi đè null lên param");
        }

        [Test]
        public void HeartbeatWithoutStart_OpensImplicitTurn()
        {
            var state = new LevelTurnState();
            var heartBeat = new LevelHeartBeatParamV2 { currentLevel = 3 };
            var anomaly = state.Apply(heartBeat);

            Assert.AreEqual(LevelTurnState.Anomaly.EventWithoutStart, anomaly);
            Assert.IsNotNull(heartBeat.playTurnId);
            Assert.AreEqual(heartBeat.playTurnId, state.OpenPlayTurnId, "Heartbeat = turn đang chơi dở → phase Open");
        }
    }
}
