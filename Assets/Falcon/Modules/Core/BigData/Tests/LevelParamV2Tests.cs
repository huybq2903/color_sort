using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class LevelParamV2Tests
    {
        private static LevelPassParamV2 NewPassParam()
        {
            return new LevelPassParamV2
            {
                playTurnId = "turn-1",
                currentLevel = 5,
                difficulty = "Hard",
                duration = TimeSpan.FromSeconds(90)
            };
        }

        [Test]
        public void ToDictionary_StatusAlwaysEmitted_WhenExtraMetaNull()
        {
            var dict = NewPassParam().ToDictionary();
            Assert.IsTrue(dict.ContainsKey("status"));
            Assert.AreEqual(LevelStatus.Pass, dict["status"]);
        }

        [Test]
        public void ToDictionary_StatusNotOverridableByExtraMeta()
        {
            var param = NewPassParam();
            param.extraMeta = new Dictionary<string, object> { ["status"] = "Hacked" };
            var dict = param.ToDictionary();
            Assert.AreEqual(LevelStatus.Pass, dict["status"]);
        }

        [Test]
        public void ToDictionary_ExtraMetaMerged_ButExistingFieldsNotOverridden()
        {
            var param = NewPassParam();
            param.extraMeta = new Dictionary<string, object>
            {
                ["coinSpend"] = 100,
                ["playTurnId"] = "fake-turn",
                ["nullValueKey"] = null
            };
            var dict = param.ToDictionary();
            Assert.AreEqual(100, dict["coinSpend"]);
            Assert.AreEqual("turn-1", dict["playTurnId"]);
            Assert.IsFalse(dict.ContainsKey("nullValueKey"));
        }

        [Test]
        public void ToDictionary_DurationEmittedAsTotalSeconds()
        {
            var dict = NewPassParam().ToDictionary();
            Assert.AreEqual(90L, dict["duration"]);
        }

        [Test]
        public void ToDictionary_IgnoredAndNullRemovedFieldsAbsent()
        {
            var param = NewPassParam();
            param.currentLevelId = null;
            var dict = param.ToDictionary();
            Assert.IsFalse(dict.ContainsKey("autoCheckComeBackAfterFirstPass"), "FKey(Ignore) field must not be encoded");
            Assert.IsFalse(dict.ContainsKey("currentLevelId"), "FKey(RemoveIfNull) null field must not be encoded");
        }

        [Test]
        public void LevelId_CombinesLevelAndDifficulty()
        {
            var param = NewPassParam();
            Assert.AreEqual("5_Difficulty_Hard", param.LevelId);
        }

        [Test]
        public void StatusPerParamType_IsCorrect()
        {
            Assert.AreEqual(LevelStatus.Start, new LevelStartParamV2().Status);
            Assert.AreEqual(LevelStatus.Pass, new LevelPassParamV2().Status);
            Assert.AreEqual(LevelStatus.Fail, new LevelFailParamV2().Status);
            Assert.AreEqual(LevelStatus.HeartBeat, new LevelHeartBeatParamV2().Status);
        }

        [Test]
        public void LevelPassParamV2_DefaultsProgressTo100()
        {
            Assert.AreEqual(100, new LevelPassParamV2().levelProgress);
        }

        [Test]
        public void ComeBackAfterFirstPass_DefaultsFalse_AndIsAutoCheckedByDefault()
        {
            // Cờ này là thứ DUY NHẤT phân biệt replay với lượt chơi đầu (§E: Re* đã khai tử).
            // Mặc định để SDK tự điền — game không phải nhớ set.
            var param = NewPassParam();
            Assert.IsFalse(param.comeBackAfterFirstPass);
            Assert.IsTrue(param.autoCheckComeBackAfterFirstPass);
        }

        [Test]
        public void ComeBackAfterFirstPass_IsEmittedInPayload()
        {
            var param = NewPassParam();
            param.comeBackAfterFirstPass = true;
            Assert.AreEqual(true, param.ToDictionary()["comeBackAfterFirstPass"]);
        }

        [Test]
        public void LevelDesignLimits_AreNotFlatOnTheTurnEvent()
        {
            // Amendment hợp đồng 2026-08-11 (§H6): moves_limit/time_limit_sec tả BẢN THIẾT KẾ MÀN
            // chứ không tả lượt chơi, nên chúng đi trong bundle level_labels dưới tên hợp đồng.
            // Thả flat lên bản tin lượt là trộn chủ thể — cook không biết field nào argMax lên trục nào.
            var dict = NewPassParam().ToDictionary();

            Assert.IsFalse(dict.ContainsKey("movesLimit"));
            Assert.IsFalse(dict.ContainsKey("timeLimitSec"));
            Assert.IsFalse(dict.ContainsKey(FLevelLabelKey.MOVES_LIMIT));
            Assert.IsFalse(dict.ContainsKey(FLevelLabelKey.TIME_LIMIT_SEC));
        }

        [Test]
        public void RegisteredLabelKeys_UseTheContractSpelling()
        {
            // Đây là chữ trên dây, không phải tên field C# — loader đọc level_labels.<key> đúng
            // tên này và cả fleet phải giống nhau thì mới so chéo game được
            Assert.AreEqual("moves_limit", FLevelLabelKey.MOVES_LIMIT);
            Assert.AreEqual("time_limit_sec", FLevelLabelKey.TIME_LIMIT_SEC);
        }

        [Test]
        public void FailReason_OnlyOnFailParam_AbsentWhenNull()
        {
            var noReason = new LevelFailParamV2().ToDictionary();
            Assert.IsFalse(noReason.ContainsKey("failReason"));

            var param = new LevelFailParamV2 { failReason = LevelFailReason.OutOfMoves };
            Assert.AreEqual(LevelFailReason.OutOfMoves, param.ToDictionary()["failReason"]);
        }

        [Test]
        public void CorrectValues_ClampsLevelProgressOver100()
        {
            var param = NewPassParam();
            param.levelProgress = 150;
            LogAssert.Expect(LogType.Error, new Regex("levelProgress"));
            param.CorrectValues();
            Assert.AreEqual(100, param.levelProgress);
        }

        [Test]
        public void CorrectValues_NegativeDuration_ResetToZero()
        {
            var param = NewPassParam();
            param.duration = TimeSpan.FromSeconds(-10);
            LogAssert.Expect(LogType.Error, new Regex("duration"));
            param.CorrectValues();
            Assert.AreEqual(TimeSpan.Zero, param.duration);
        }

        [Test]
        public void CorrectValues_NegativeBoosterCount_RemovedFromDictionary()
        {
            var param = NewPassParam();
            param.boostersUsed = new Dictionary<string, int> { ["hammer"] = 2, ["bomb"] = -1 };
            LogAssert.Expect(LogType.Error, new Regex("bomb"));
            param.CorrectValues();
            Assert.IsTrue(param.boostersUsed.ContainsKey("hammer"));
            Assert.IsFalse(param.boostersUsed.ContainsKey("bomb"));
        }
    }
}
