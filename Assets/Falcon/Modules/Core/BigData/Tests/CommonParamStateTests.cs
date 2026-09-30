using System;
using System.Collections.Generic;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class CommonParamStateTests
    {

        [Test]
        public void WireColumnName_IsRejected_NotStored()
        {
            // §H1: key trùng cột hợp đồng bị cột thật đè trên event có cột đó và chỉ xuất hiện
            // trên phần còn lại — nửa có nửa không. Chặn từ cửa, mọi cách viết.
            var state = new CommonParamState();

            Assert.AreEqual(CommonParamSet.WireKeyRejected, state.Set("win_streak", 5, persist: false));
            Assert.AreEqual(CommonParamSet.WireKeyRejected, state.Set("winStreak", () => 5));
            Assert.IsFalse(state.Has("win_streak"));
            Assert.IsFalse(state.Has("winStreak"));
        }

        [Test]
        public void Restore_SkipsKeysThatBecameWireColumns()
        {
            // Key persist từ bản SDK cũ có thể đụng cột mà bản mới vừa thêm vào hợp đồng
            var state = new CommonParamState();

            state.Restore(new Dictionary<string, object> { ["win_streak"] = 9, ["guild_id"] = "g1" });

            Assert.IsFalse(state.Has("win_streak"));
            Assert.IsTrue(state.Has("guild_id"));
        }
        [Test]
        public void StaticValue_StaysOnEveryLog()
        {
            var state = new CommonParamState();
            state.Set("guildId", "g_42", persist: false);

            Assert.AreEqual("g_42", state.BuildInfo()["guildId"]);
            Assert.AreEqual("g_42", state.BuildInfo()["guildId"], "Không phải one-shot");
        }

        [Test]
        public void Provider_IsReadFreshEachTime()
        {
            var state = new CommonParamState();
            var gold = 100;
            state.Set("gold", () => gold);

            Assert.AreEqual(100, state.BuildInfo()["gold"]);
            gold = 250;
            Assert.AreEqual(250, state.BuildInfo()["gold"], "Giá trị mới nhất, game khỏi nhớ set lại");
        }

        [Test]
        public void NullValue_RemovesTheKey()
        {
            // §H4: vắng mặt khỏi payload chứ không gửi null
            var state = new CommonParamState();
            state.Set("guildId", "g_42", persist: true);

            state.Set("guildId", null, persist: true);

            Assert.IsFalse(state.Has("guildId"));
            Assert.IsEmpty(state.PersistedValues);
        }

        [Test]
        public void ProviderReturningNull_IsSkippedButKeyStays()
        {
            var state = new CommonParamState();
            string guild = null;
            state.Set("guildId", () => guild);

            Assert.IsFalse(state.BuildInfo().ContainsKey("guildId"), "Chưa vào guild thì vắng mặt");

            guild = "g_42";
            Assert.AreEqual("g_42", state.BuildInfo()["guildId"], "Vào rồi thì có, không phải khai lại");
        }

        [Test]
        public void ThrowingProvider_DoesNotKillTheOtherKeys()
        {
            // Một key hỏng không được phép làm chết cả đường log
            var state = new CommonParamState();
            state.Set("broken", () => throw new InvalidOperationException("boom"));
            state.Set("fine", "ok", persist: false);

            string failedKey = null;
            var info = state.BuildInfo((key, _) => failedKey = key);

            Assert.AreEqual("broken", failedKey);
            Assert.IsFalse(info.ContainsKey("broken"));
            Assert.AreEqual("ok", info["fine"]);
        }

        [Test]
        public void CentralKey_IsRejected_NotSilentlyIgnored()
        {
            // Lúc merge, tham số trung tâm luôn thắng (PutIfAbsent) nên key trùng sẽ vô hình —
            // từ chối ngay để dev không ngồi đoán vì sao giá trị của mình không ra
            var state = new CommonParamState();

            Assert.AreEqual(CommonParamSet.CentralKeyRejected, state.Set("level", 99, persist: false));
            Assert.AreEqual(CommonParamSet.CentralKeyRejected, state.Set("sessionUid", () => "x"));
            Assert.IsFalse(state.Has("level"));
        }

        [Test]
        public void SetTwice_LastValueWins()
        {
            var state = new CommonParamState();
            state.Set("tier", 1, persist: false);
            state.Set("tier", 5, persist: false);

            Assert.AreEqual(5, state.BuildInfo()["tier"]);
        }

        [Test]
        public void BlankKeyOrNullProvider_IsIgnored()
        {
            var state = new CommonParamState();

            Assert.AreEqual(CommonParamSet.Ignored, state.Set(null, "x", persist: false));
            Assert.AreEqual(CommonParamSet.Ignored, state.Set(string.Empty, "x", persist: false));
            Assert.AreEqual(CommonParamSet.Ignored, state.Set("k", (Func<object>)null));
            Assert.IsEmpty(state.BuildInfo());
        }

        // ---- Sống qua restart (opt-in) ----

        [Test]
        public void OnlyPersistFlaggedKeys_AreKeptForNextLaunch()
        {
            var state = new CommonParamState();
            state.Set("cohort", "abc", persist: true);
            state.Set("guildId", "g_42", persist: false);

            var saved = state.PersistedValues;

            Assert.AreEqual("abc", saved["cohort"]);
            Assert.IsFalse(saved.ContainsKey("guildId"), "Mặc định là RAM, không tự lưu");
        }

        [Test]
        public void Restore_BringsKeysBackBeforeGameRuns()
        {
            // Đây là điểm của persist: key có mặt ngay từ log ĐẦU phiên
            var restarted = new CommonParamState();
            restarted.Restore(new Dictionary<string, object> { ["cohort"] = "abc" });

            Assert.AreEqual("abc", restarted.BuildInfo()["cohort"]);
            Assert.AreEqual("abc", restarted.PersistedValues["cohort"], "Vẫn tiếp tục được lưu");
        }

        [Test]
        public void Restore_DoesNotOverrideWhatGameAlreadySetThisSession()
        {
            // Giá trị của phiên này luôn đúng hơn giá trị của phiên trước
            var state = new CommonParamState();
            state.Set("cohort", "new", persist: true);

            state.Restore(new Dictionary<string, object> { ["cohort"] = "old" });

            Assert.AreEqual("new", state.BuildInfo()["cohort"]);
        }

        [Test]
        public void Restore_SkipsCentralKeys()
        {
            // File save cũ có thể mang key mà nay Devkit đã lấy làm tham số trung tâm
            var state = new CommonParamState();

            state.Restore(new Dictionary<string, object> { ["level"] = 99, ["ok"] = 1 });

            Assert.IsFalse(state.Has("level"));
            Assert.IsTrue(state.Has("ok"));
        }

        [Test]
        public void SetWithoutPersist_DropsThePreviouslyPersistedValue()
        {
            // Đổi ý: key này không còn đáng sống qua restart nữa
            var state = new CommonParamState();
            state.Set("cohort", "abc", persist: true);

            state.Set("cohort", "abc", persist: false);

            Assert.IsTrue(state.Has("cohort"));
            Assert.IsEmpty(state.PersistedValues);
        }

        [Test]
        public void Provider_ClearsPersistedValueOfTheSameKey()
        {
            // Provider không lưu được xuống đĩa, nên để lại bản cũ là lần sau khởi động ăn giá trị chết
            var state = new CommonParamState();
            state.Set("tier", 3, persist: true);

            state.Set("tier", () => 7);

            Assert.AreEqual(7, state.BuildInfo()["tier"]);
            Assert.IsEmpty(state.PersistedValues);
        }

        [Test]
        public void Remove_AlsoClearsThePersistedCopy()
        {
            var state = new CommonParamState();
            state.Set("cohort", "abc", persist: true);

            state.Remove("cohort");

            Assert.IsFalse(state.Has("cohort"));
            Assert.IsEmpty(state.PersistedValues);
        }
    }
}
