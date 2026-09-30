using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class AppOpenStateTests
    {
        private const int THRESHOLD = 30;
        private const long T0 = 1_000_000L;

        private static long Sec(int s) => T0 + s * 1000L;

        [Test]
        public void FirstOpen_IsCold_NoBackgroundDuration()
        {
            var state = new AppOpenState();
            var param = state.TryOpen(T0, THRESHOLD);

            Assert.IsNotNull(param);
            Assert.AreEqual(LaunchType.Cold, param.launchType);
            Assert.IsNull(param.backgroundDurationSec, "Cold không có nền để đo");
            Assert.AreEqual(1, param.openIndex);
        }

        [Test]
        public void ColdOpen_NeverFiltered_EvenWithHugeThreshold()
        {
            var state = new AppOpenState();
            Assert.IsNotNull(state.TryOpen(T0, 999_999));
        }

        [Test]
        public void LongBackground_IsHot_WithDuration()
        {
            var state = new AppOpenState();
            state.TryOpen(T0, THRESHOLD);
            state.MarkPause(Sec(10));

            var param = state.TryOpen(Sec(130), THRESHOLD);

            Assert.IsNotNull(param);
            Assert.AreEqual(LaunchType.Hot, param.launchType);
            Assert.AreEqual(120, param.backgroundDurationSec);
            Assert.AreEqual(2, param.openIndex);
        }

        [Test]
        public void ShortBackground_IsFiltered()
        {
            // Thoát ra 2 giây copy OTP → không log
            var state = new AppOpenState();
            state.TryOpen(T0, THRESHOLD);
            state.MarkPause(Sec(10));

            Assert.IsNull(state.TryOpen(Sec(12), THRESHOLD));
        }

        [Test]
        public void ExactlyAtThreshold_IsLogged()
        {
            var state = new AppOpenState();
            state.TryOpen(T0, THRESHOLD);
            state.MarkPause(Sec(10));

            var param = state.TryOpen(Sec(10 + THRESHOLD), THRESHOLD);
            Assert.IsNotNull(param, "Đúng bằng ngưỡng thì vẫn log (chỉ dưới ngưỡng mới lọc)");
            Assert.AreEqual(THRESHOLD, param.backgroundDurationSec);
        }

        [Test]
        public void FilteredOpens_StillAdvanceOpenIndex()
        {
            // Khoảng nhảy của openIndex là dấu vết của những lần bị lọc — server đếm được
            var state = new AppOpenState();
            state.TryOpen(T0, THRESHOLD);                       // index 1 (cold)

            state.MarkPause(Sec(10));
            Assert.IsNull(state.TryOpen(Sec(12), THRESHOLD));   // index 2 — lọc
            state.MarkPause(Sec(20));
            Assert.IsNull(state.TryOpen(Sec(22), THRESHOLD));   // index 3 — lọc

            state.MarkPause(Sec(30));
            var param = state.TryOpen(Sec(200), THRESHOLD);     // index 4 — log
            Assert.IsNotNull(param);
            Assert.AreEqual(4, param.openIndex, "Có 2 lần bị lọc giữa index 1 và 4");
        }

        [Test]
        public void ZeroThreshold_LogsEveryReturn()
        {
            var state = new AppOpenState();
            state.TryOpen(T0, 0);
            state.MarkPause(Sec(10));

            var param = state.TryOpen(Sec(11), 0);
            Assert.IsNotNull(param);
            Assert.AreEqual(1, param.backgroundDurationSec);
        }

        [Test]
        public void HotOpenWithoutPauseMark_HasNullDuration_AndIsLogged()
        {
            // Bất thường (AppFlowService luôn stop trước continue) — để null thay vì bịa số 0
            var state = new AppOpenState();
            state.TryOpen(T0, THRESHOLD);

            var param = state.TryOpen(Sec(5), THRESHOLD);
            Assert.IsNotNull(param);
            Assert.AreEqual(LaunchType.Hot, param.launchType);
            Assert.IsNull(param.backgroundDurationSec);
        }

        [Test]
        public void PauseMark_IsConsumed_NotReusedByNextOpen()
        {
            var state = new AppOpenState();
            state.TryOpen(T0, THRESHOLD);
            state.MarkPause(Sec(10));
            state.TryOpen(Sec(100), THRESHOLD);              // dùng mốc pause

            var param = state.TryOpen(Sec(200), THRESHOLD);  // không có pause mới
            Assert.IsNotNull(param);
            Assert.IsNull(param.backgroundDurationSec, "Mốc pause cũ không được dùng lại");
        }

        [Test]
        public void ReportedSource_LandsOnTheOpenThatFollows()
        {
            var state = new AppOpenState();
            state.ReportSource(OpenSource.Push, "camp_42", T0);

            var param = state.TryOpen(Sec(1), THRESHOLD);

            Assert.AreEqual(OpenSource.Push, param.openSource);
            Assert.AreEqual("camp_42", param.pushCampaignId);
        }

        [Test]
        public void CampaignId_OnlyKeptForPush()
        {
            // Deeplink mang campaign id là lẫn dữ liệu — server đọc cột đó là "push nào kéo user về"
            var state = new AppOpenState();
            state.ReportSource(OpenSource.Deeplink, "camp_42", T0);

            var param = state.TryOpen(Sec(1), THRESHOLD);

            Assert.AreEqual(OpenSource.Deeplink, param.openSource);
            Assert.IsNull(param.pushCampaignId);
        }

        [Test]
        public void NoReport_LeavesSourceEmpty_NotDefaultedToIcon()
        {
            // Mặc định icon thì con số "mở từ icon" chỉ đang đếm những game chưa wire
            var param = new AppOpenState().TryOpen(T0, THRESHOLD);

            Assert.IsNull(param.openSource);
        }

        [Test]
        public void StaleReport_IsDropped_NotAppliedToALaterOpen()
        {
            // Bấm push lúc 9h rồi quay lại app lúc 11h là hai chuyện khác nhau
            var state = new AppOpenState();
            state.TryOpen(T0, THRESHOLD);
            state.ReportSource(OpenSource.Push, "camp_42", Sec(10));
            state.MarkPause(Sec(20));

            var param = state.TryOpen(Sec(10_000), THRESHOLD);

            Assert.IsNull(param.openSource);
            Assert.IsNull(param.pushCampaignId);
        }

        [Test]
        public void Report_IsConsumedOnce()
        {
            var state = new AppOpenState();
            state.ReportSource(OpenSource.Widget, null, T0);
            state.TryOpen(T0, THRESHOLD);
            state.MarkPause(Sec(10));

            var second = state.TryOpen(Sec(100), THRESHOLD);

            Assert.IsNull(second.openSource, "Nguồn của lần mở trước không dán sang lần sau");
        }

        [Test]
        public void ReportRightAfterAnOpen_IsFlaggedLate()
        {
            // Bản tin đã gửi thì không đóng dấu được nữa — cảnh báo cho dev sửa chỗ gọi
            var state = new AppOpenState();
            state.TryOpen(T0, THRESHOLD);

            Assert.IsTrue(state.ReportSource(OpenSource.Push, "camp_42", Sec(1)));
        }

        [Test]
        public void ReportBeforeAnyOpen_IsNotFlaggedLate()
        {
            Assert.IsFalse(new AppOpenState().ReportSource(OpenSource.Push, "camp_42", T0));
        }

        [Test]
        public void ParamToDictionary_OmitsNullDuration_KeepsLaunchTypeAndIndex()
        {
            var state = new AppOpenState();
            var dict = state.TryOpen(T0, THRESHOLD).ToDictionary();

            Assert.AreEqual(LaunchType.Cold, dict["launchType"]);
            Assert.AreEqual(1, dict["openIndex"]);
            Assert.IsFalse(dict.ContainsKey("backgroundDurationSec"), "null thì vắng mặt khỏi payload (§H4)");
        }

        // ── extras đi kèm nguồn mở (ReportOpenSource(extraMeta)) ────────────────

        [Test]
        public void SourceExtras_RideAlongWithTheSource()
        {
            var state = new AppOpenState();
            state.ReportSource(OpenSource.Push, "camp_42", T0,
                new System.Collections.Generic.Dictionary<string, object> { ["message_id"] = "m1" });

            var param = state.TryOpen(Sec(2), THRESHOLD);

            Assert.AreEqual("m1", param.extraMeta["message_id"]);
        }

        [Test]
        public void SourceExtras_ExpireTogetherWithTheSource()
        {
            // Nguồn quá TTL bị vứt thì extras phải vứt cùng — dán extras mồ côi lên lần mở sau
            // cũng sai y như dán nguồn
            var state = new AppOpenState();
            state.ReportSource(OpenSource.Push, "camp_42", T0,
                new System.Collections.Generic.Dictionary<string, object> { ["message_id"] = "m1" });

            var param = state.TryOpen(T0 + AppOpenState.SOURCE_REPORT_TTL_MILLIS + 1, THRESHOLD);

            Assert.IsFalse(param.openSource.HasValue);
            Assert.IsNull(param.extraMeta);
        }
    }
}
