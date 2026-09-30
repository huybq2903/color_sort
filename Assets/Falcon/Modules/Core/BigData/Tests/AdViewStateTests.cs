using Falcon.Helpers.Devkit;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class AdViewStateTests
    {
        private const long T0 = 1_000_000L;
        private static long Sec(int s) => T0 + s * 1000L;
        private static AdViewParam Request(AdType type) => new() { type = type };

        [Test]
        public void Open_GeneratesAdViewId()
        {
            // Id là thứ SDK sinh nên nó ĐI RA theo giá trị trả về, không ghi ngược vào param của
            // caller — param chỉ chứa cái game khai
            Assert.IsNotNull(new AdViewState().Open(Request(AdType.Interstitial), T0));
        }

        [Test]
        public void WholeLifecycle_SharesOneId()
        {
            // request → show → impression → close: mọi mốc đọc ra cùng một id
            var state = new AdViewState();
            var id = state.Open(Request(AdType.Reward), T0);

            Assert.AreEqual(id, state.CurrentId(AdType.Reward), "impression");
            Assert.AreEqual(id, state.MarkShown(new AdShowParam { type = AdType.Reward }, Sec(2)).adViewId, "show");
            Assert.AreEqual(id, state.MarkClosed(new AdCloseParam { type = AdType.Reward }, Sec(30)).adViewId,
                "close");
        }

        [Test]
        public void BannerRefreshImpressions_ShareIdOfTheirRequest()
        {
            // Banner tự refresh trong SDK: N impression thuộc CÙNG một banner instance
            var state = new AdViewState();
            var id = state.Open(Request(AdType.Banner), T0);

            Assert.AreEqual(id, state.CurrentId(AdType.Banner));
            Assert.AreEqual(id, state.CurrentId(AdType.Banner));
        }

        [Test]
        public void Impression_WithoutAnyRequest_GetsNothing()
        {
            Assert.IsNull(new AdViewState().CurrentId(AdType.Interstitial));
        }

        [Test]
        public void FormatsAreIndependent()
        {
            // Các format chạy song song: preload inter + preload rewarded cùng lúc
            var state = new AdViewState();
            var inter = state.Open(Request(AdType.Interstitial), T0);
            var reward = state.Open(Request(AdType.Reward), T0);

            Assert.AreEqual(reward, state.CurrentId(AdType.Reward));
            Assert.AreEqual(inter, state.CurrentId(AdType.Interstitial),
                "Impression của format này không được nuốt id của format kia");
        }

        [Test]
        public void RetryRequest_ReplacesPending_PreviousIsFillFail()
        {
            // Load fail → retry: request cũ không bao giờ lên hình = fill-fail (server thấy
            // request không có impression mang id đó), request mới chiếm slot.
            var state = new AdViewState();
            var first = state.Open(Request(AdType.Interstitial), T0);
            var retry = state.Open(Request(AdType.Interstitial), Sec(2));

            Assert.AreNotEqual(first, retry);
            Assert.AreEqual(retry, state.CurrentId(AdType.Interstitial));
        }

        [Test]
        public void Show_CarriesIdAndMeasuresWaitFromRequest()
        {
            var state = new AdViewState();
            var id = state.Open(Request(AdType.Interstitial), T0);

            var shown = state.MarkShown(new AdShowParam { type = AdType.Interstitial }, Sec(3));

            Assert.AreEqual(id, shown.adViewId);
            Assert.AreEqual(3000, shown.requestToShowMs);
        }

        [Test]
        public void Show_WithoutRequest_LeavesIdAndWaitEmpty()
        {
            // Đường load nằm ngoài SDK → không bịa số
            var shown = new AdViewState().MarkShown(new AdShowParam { type = AdType.Interstitial }, T0);

            Assert.IsNull(shown.adViewId);
            Assert.IsNull(shown.requestToShowMs);
        }

        [Test]
        public void Close_CarriesIdAndMeasuresShownDuration()
        {
            var state = new AdViewState();
            var id = state.Open(Request(AdType.Reward), T0);
            state.MarkShown(new AdShowParam { type = AdType.Reward }, Sec(2));

            var closed = state.MarkClosed(new AdCloseParam { type = AdType.Reward, adCompleted = true }, Sec(32));

            Assert.AreEqual(id, closed.adViewId);
            Assert.AreEqual(30, closed.shownDurationSec);
        }

        [Test]
        public void Close_WithoutShow_LeavesDurationEmpty()
        {
            // Request rồi đóng mà chưa từng hiển thị (huỷ preload) → không có thời lượng để đo
            var state = new AdViewState();
            state.Open(Request(AdType.Interstitial), T0);

            var closed = state.MarkClosed(new AdCloseParam { type = AdType.Interstitial }, Sec(5));

            Assert.IsNotNull(closed.adViewId);
            Assert.IsNull(closed.shownDurationSec);
        }

        [Test]
        public void MillisSinceRequest_MeasuresFillLatency()
        {
            var state = new AdViewState();
            state.Open(Request(AdType.Reward), T0);

            Assert.AreEqual(4000, state.MillisSinceRequest(AdType.Reward, Sec(4)));
        }

        [Test]
        public void MillisSinceRequest_WithoutRequest_IsNull()
        {
            // Impression không đi từ request nào của SDK → không bịa 0
            Assert.IsNull(new AdViewState().MillisSinceRequest(AdType.Interstitial, T0));
        }

        [Test]
        public void Placement_SetAfterRequest_FlowsToShowAndClose()
        {
            // Preload lúc chưa biết chiếu ở đâu; biết lúc nào ghi lúc đó
            var state = new AdViewState();
            state.Open(Request(AdType.Interstitial), T0);

            Assert.IsTrue(state.SetContext(AdType.Interstitial, adWhere: "level_end"));

            var show = new AdShowParam { type = AdType.Interstitial };
            state.MarkShown(show, Sec(2));
            var close = new AdCloseParam { type = AdType.Interstitial };
            state.MarkClosed(close, Sec(20));

            Assert.AreEqual("level_end", show.adWhere);
            Assert.AreEqual("level_end", close.adWhere);
            Assert.AreEqual("level_end", state.CurrentPlacement(AdType.Interstitial));
        }

        [Test]
        public void PlacementOnShow_WinsAndUpdatesTheSlot()
        {
            // Mốc nào tự nhập thì giá trị đó thắng — và các mốc sau theo luôn
            var state = new AdViewState();
            state.Open(Request(AdType.Interstitial), T0);
            state.SetContext(AdType.Interstitial, adWhere: "level_end");

            var show = new AdShowParam { type = AdType.Interstitial, adWhere = "shop" };
            state.MarkShown(show, Sec(2));
            var close = new AdCloseParam { type = AdType.Interstitial };
            state.MarkClosed(close, Sec(9));

            Assert.AreEqual("shop", show.adWhere);
            Assert.AreEqual("shop", close.adWhere);
        }

        [Test]
        public void Placement_WithoutRequest_IsRejected()
        {
            var state = new AdViewState();

            Assert.IsFalse(state.SetContext(AdType.Reward, adWhere: "level_end"));
            Assert.IsNull(state.CurrentPlacement(AdType.Reward));
        }

        [Test]
        public void Placement_DoesNotSurviveIntoTheNextRequest()
        {
            // Request mới là lần xem ad khác — chỗ chiếu cũ không được dính sang
            var state = new AdViewState();
            state.Open(Request(AdType.Reward), T0);
            state.SetContext(AdType.Reward, adWhere: "double_coin");

            state.Open(Request(AdType.Reward), Sec(60));

            Assert.IsNull(state.CurrentPlacement(AdType.Reward));
        }

        [Test]
        public void Mediation_CachedFromRequest_FlowsToLaterMarks()
        {
            // adMediation bất biến trong cả vòng đời một view — nhập một lần lúc request là đủ
            var state = new AdViewState();
            var request = new AdViewParam { type = AdType.Reward, adMediation = "IronSource" };
            state.Open(request, T0);

            var show = new AdShowParam { type = AdType.Reward };
            state.MarkShown(show, Sec(2));
            var close = new AdCloseParam { type = AdType.Reward };
            state.MarkClosed(close, Sec(30));

            Assert.AreEqual("IronSource", show.adMediation);
            Assert.AreEqual("IronSource", close.adMediation);
        }

        [Test]
        public void Context_CachedFromRequest_FlowsToLaterMarks()
        {
            var state = new AdViewState();
            var request = new AdViewParam { type = AdType.Interstitial, adWhen = "level_fail" };
            state.Open(request, T0);

            var show = new AdShowParam { type = AdType.Interstitial };
            state.MarkShown(show, Sec(1));

            Assert.AreEqual("level_fail", show.adWhen);
            Assert.AreEqual("level_fail", state.CurrentContext(AdType.Interstitial));
        }

        [Test]
        public void Snapshot_ReportsWholeContextAndTimings()
        {
            var state = new AdViewState();
            state.Open(new AdViewParam { type = AdType.Reward, adMediation = "Max" }, T0);
            state.SetContext(AdType.Reward, adWhere: "shop", adWhen: "out_of_life");
            state.MarkShown(new AdShowParam { type = AdType.Reward }, Sec(3));

            var snapshot = state.TakeSnapshot(AdType.Reward, Sec(10));

            Assert.AreEqual(state.CurrentId(AdType.Reward), snapshot.adViewId);
            Assert.AreEqual("shop", snapshot.adWhere);
            Assert.AreEqual("out_of_life", snapshot.adWhen);
            Assert.AreEqual("Max", snapshot.adMediation);
            Assert.AreEqual(10000, snapshot.sinceRequestMs);
            Assert.AreEqual(7000, snapshot.sinceShownMs);
        }

        [Test]
        public void Snapshot_BeforeShow_HasNoShownDuration()
        {
            var state = new AdViewState();
            state.Open(Request(AdType.Banner), T0);

            Assert.IsNull(state.TakeSnapshot(AdType.Banner, Sec(5)).sinceShownMs);
        }

        [Test]
        public void Snapshot_WithoutRequest_IsNull()
        {
            Assert.IsNull(new AdViewState().TakeSnapshot(AdType.AppOpen, T0));
        }

        [Test]
        public void RequestParam_OmitsNullOptionalFields()
        {
            var state = new AdViewState();
            var param = Request(AdType.AppOpen);
            state.Open(param, T0);

            var dict = param.ToDictionary();
            Assert.AreEqual(AdType.AppOpen, dict["type"]);
            Assert.IsFalse(dict.ContainsKey("adViewId"), "adViewId là của SDK nên nằm trên log, không nằm ở param");
            Assert.IsFalse(dict.ContainsKey("adWhere"), "null thì vắng mặt khỏi payload (§H4)");
            Assert.IsFalse(dict.ContainsKey("adMediation"));
        }

        [Test]
        public void UnknownContext_CountsAsBlank_DoesNotWipeTheCache()
        {
            // AdParam.CorrectValues() điền sẵn "Unknown" cho log impression trước khi decor chạy;
            // coi đó là giá trị thật thì cache mất luôn chỗ chiếu game đã báo lúc request
            var state = new AdViewState();
            state.Open(new AdViewParam { type = AdType.Banner, adWhere = "main_menu" }, T0);

            var impression = new AdParam { type = AdType.Banner, adWhere = FParam.UNKNOWN, adWhen = FParam.UNKNOWN };
            state.ApplyContext(impression, AdType.Banner);

            Assert.AreEqual("main_menu", impression.adWhere);
            Assert.AreEqual("main_menu", state.CurrentPlacement(AdType.Banner));
        }

        // ── OnClicked: mốc thống nhất, ruột là cờ đóng lên close ────────────────

        [Test]
        public void Clicked_StampsHasClickOnClose()
        {
            var state = new AdViewState();
            state.Open(Request(AdType.Interstitial), T0);

            Assert.IsTrue(state.MarkClicked(AdType.Interstitial));
            var close = new AdCloseParam { type = AdType.Interstitial };
            state.MarkClosed(close, Sec(30));

            Assert.IsTrue(close.hasClick, "Cờ click phải tự chảy vào hasClick của log close");
        }

        [Test]
        public void ExplicitHasClick_BeatsTheClickedFlag()
        {
            // Mediation tự truyền hasClick (kể cả false) thì giá trị tường minh thắng cờ
            var state = new AdViewState();
            state.Open(Request(AdType.Interstitial), T0);
            state.MarkClicked(AdType.Interstitial);

            var close = new AdCloseParam { type = AdType.Interstitial, hasClick = false };
            state.MarkClosed(close, Sec(30));

            Assert.IsFalse(close.hasClick);
        }

        [Test]
        public void NoClickSeen_SendsHasClickFalse_Explicitly()
        {
            // Sửa 23/09 (đo của loader: 0 dòng nào mang false): cờ click là số đo thật của SDK nên
            // chưa thấy click phải gửi false TƯỜNG MINH — để trống thì bên đọc số không phân biệt
            // được "không bấm" với "không biết", và phải tự COALESCE.
            var state = new AdViewState();
            state.Open(Request(AdType.Reward), T0);

            var close = new AdCloseParam { type = AdType.Reward };
            state.MarkClosed(close, Sec(30));

            Assert.IsFalse(close.hasClick);
        }

        [Test]
        public void ClickedFlag_DoesNotLeakIntoTheNextView()
        {
            var state = new AdViewState();
            state.Open(Request(AdType.Interstitial), T0);
            state.MarkClicked(AdType.Interstitial);
            state.MarkClosed(new AdCloseParam { type = AdType.Interstitial }, Sec(30));

            state.Open(Request(AdType.Interstitial), Sec(60)); // request mới = view mới
            var close = new AdCloseParam { type = AdType.Interstitial };
            state.MarkClosed(close, Sec(90));

            Assert.IsFalse(close.hasClick, "Cờ click của view trước không được dây sang view sau (false, không phải true)");
        }

        [Test]
        public void Click_WithoutAnyView_IsRejected()
        {
            Assert.IsFalse(new AdViewState().MarkClicked(AdType.Interstitial));
        }

        // ---- Luật xoay id khi view đã tiêu thụ (án dup 5% của loader, 03/09) ----

        [Test]
        public void FirstImpression_KeepsRequestId_AndMeasuresFill()
        {
            var state = new AdViewState();
            var id = state.Open(Request(AdType.Interstitial), T0);

            var stamp = state.StampImpression(AdType.Interstitial, Sec(3));

            Assert.AreEqual(id, stamp.adViewId, "Impression đầu phải giữ id của request — phễu fill nguyên vẹn");
            Assert.AreEqual(3000, stamp.fillLatencyMs);
            Assert.IsFalse(stamp.rotated);
        }

        [Test]
        public void SecondImpression_SameView_RotatesToFreshId()
        {
            // Mediation quên OnRequested đợt mới: hai impression đổ vào cùng view — SDK tự xoay
            // (khuôn play_turn_id) thay vì để một id bị tính tiền hai lần
            var state = new AdViewState();
            var id1 = state.Open(Request(AdType.Interstitial), T0);
            state.StampImpression(AdType.Interstitial, Sec(3));

            var second = state.StampImpression(AdType.Interstitial, Sec(120));

            Assert.IsNotNull(second.adViewId, "Vẫn đủ tham số — id sinh mới chứ không vắng mặt");
            Assert.AreNotEqual(id1, second.adViewId);
            Assert.IsTrue(second.rotated, "Caller cần biết để cảnh báo thiếu OnRequested");
            Assert.IsNull(second.fillLatencyMs, "View tự sinh không đi từ request nào — không bịa số đo");
        }

        [Test]
        public void ShowOnConsumedView_RotatesForTheWholeNextDisplay()
        {
            // Có gọi OnShown: xoay ngay tại show để cả bộ show/impression/close của lần chiếu mới
            // đi chung MỘT id mới — không nửa nạc show-id-cũ imp-id-mới
            var state = new AdViewState();
            var id1 = state.Open(Request(AdType.Reward), T0);
            state.MarkShown(new AdShowParam { type = AdType.Reward }, Sec(2));
            state.StampImpression(AdType.Reward, Sec(2));
            state.MarkClosed(new AdCloseParam { type = AdType.Reward }, Sec(30));

            var shown2 = state.MarkShown(new AdShowParam { type = AdType.Reward }, Sec(60));
            var imp2 = state.StampImpression(AdType.Reward, Sec(60));
            var close2 = state.MarkClosed(new AdCloseParam { type = AdType.Reward }, Sec(90));

            Assert.IsTrue(shown2.rotated);
            Assert.AreNotEqual(id1, shown2.adViewId);
            Assert.IsNull(shown2.requestToShowMs, "Không đi từ request nào — null, không bịa");
            Assert.AreEqual(shown2.adViewId, imp2.adViewId, "Impression của lần chiếu mới đi id mới");
            Assert.IsFalse(imp2.rotated, "Đã xoay ở show rồi — impression đầu của view mới không xoay nữa");
            Assert.AreEqual(shown2.adViewId, close2.adViewId, "Close cũng theo id mới");
        }

        [Test]
        public void CloseAfterImpression_NeverRotates()
        {
            // Vòng đời chuẩn request → show → imp → close: close chạm view đã tiêu thụ là HỢP LỆ
            var state = new AdViewState();
            var id = state.Open(Request(AdType.Interstitial), T0);
            state.MarkShown(new AdShowParam { type = AdType.Interstitial }, Sec(2));
            state.StampImpression(AdType.Interstitial, Sec(2));

            Assert.AreEqual(id, state.MarkClosed(new AdCloseParam { type = AdType.Interstitial }, Sec(30)).adViewId);
        }

        [Test]
        public void Banner_NeverRotates_InstanceIdByContract()
        {
            // Id banner là id INSTANCE — N impression chung id là hợp đồng (bản tin cụm mang
            // impressionCount), xoay là phá ngữ nghĩa cụm
            var state = new AdViewState();
            var id = state.Open(Request(AdType.Banner), T0);

            var first = state.StampImpression(AdType.Banner, Sec(30));
            var second = state.StampImpression(AdType.Banner, Sec(60));

            Assert.AreEqual(id, first.adViewId);
            Assert.AreEqual(id, second.adViewId);
            Assert.IsFalse(second.rotated);
        }

        [Test]
        public void RotatedView_DoesNotInheritClick()
        {
            // Cờ click của lần chiếu trước không được lây sang close của lần chiếu sau khi xoay
            var state = new AdViewState();
            state.Open(Request(AdType.Interstitial), T0);
            state.StampImpression(AdType.Interstitial, Sec(2));
            state.MarkClicked(AdType.Interstitial);

            state.StampImpression(AdType.Interstitial, Sec(60)); // xoay
            var close = new AdCloseParam { type = AdType.Interstitial };
            state.MarkClosed(close, Sec(90));

            Assert.IsFalse(close.hasClick, "View xoay bắt đầu sạch cờ click — false, không phải kế thừa true");
        }
    }
}
