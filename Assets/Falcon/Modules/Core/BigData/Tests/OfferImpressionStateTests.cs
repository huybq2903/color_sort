using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class OfferImpressionStateTests
    {
        private const long T0 = 1_000_000L;
        private const long GRACE = 120_000L;   // 120s
        private const string PRODUCT = "com.game.gem_pack_1";

        private static long Sec(int s) => T0 + s * 1000L;

        private static IapOfferParam Offer(params string[] products)
        {
            return new IapOfferParam
            {
                offerId = "black_friday_gem",
                offerProductId = products.Length > 0 ? products : null
            };
        }

        [Test]
        public void Open_GeneratesImpressionId()
        {
            var state = new OfferImpressionState();

            var (id, replaced) = state.Open(Offer(PRODUCT), T0, GRACE);

            Assert.IsNull(replaced, "Không có offer nào trước đó");
            Assert.IsNotNull(id);
            Assert.AreEqual(id, state.OpenImpressionId);
        }

        [Test]
        public void Close_ReturnsParamWithDuration_AndClearsOpenState()
        {
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);

            var closed = state.Close(Sec(12), GRACE);

            Assert.IsNotNull(closed);
            Assert.AreEqual(12L, closed.impressionDurationSec);
            Assert.IsNull(state.OpenImpressionId);
        }

        [Test]
        public void Close_WithoutOpenOffer_ReturnsNull()
        {
            Assert.IsNull(new OfferImpressionState().Close(T0, GRACE));
        }

        [Test]
        public void MarkClicked_SetsFlagOnOpenOffer()
        {
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);
            state.MarkClicked();

            Assert.IsTrue(state.Close(Sec(1), GRACE).param.isClicked);
        }

        [Test]
        public void TryAttribute_MatchingProduct_ReturnsIdAndMarksPurchased()
        {
            var state = new OfferImpressionState();
            var (id, _) = state.Open(Offer(PRODUCT, "com.game.gem_pack_2"), T0, GRACE);

            Assert.AreEqual(id, state.TryAttribute(PRODUCT, Sec(3)));
            Assert.IsTrue(state.Close(Sec(5), GRACE).param.isPurchased);
        }

        [Test]
        public void TryAttribute_OtherProduct_ReturnsNull()
        {
            // Offer card nằm lì trên UI không được ăn nhầm giao dịch của sản phẩm khác
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);

            Assert.IsNull(state.TryAttribute("com.game.something_else", Sec(3)));
            Assert.IsFalse(state.Close(Sec(5), GRACE).param.isPurchased.Value);
        }

        [Test]
        public void TryAttribute_NoProductsDeclared_ReturnsNull()
        {
            var state = new OfferImpressionState();
            state.Open(Offer(), T0, GRACE);

            Assert.IsNull(state.TryAttribute(PRODUCT, Sec(3)), "Không khai offerProductId thì không quy được");
        }

        [Test]
        public void TryAttribute_NoOpenOffer_ReturnsNull()
        {
            Assert.IsNull(new OfferImpressionState().TryAttribute(PRODUCT, T0));
        }

        [Test]
        public void Close_NoProductsDeclared_LeavesIsPurchasedNull()
        {
            // Không có gì để đối chiếu → "không biết", KHÔNG khẳng định false (§H4)
            var state = new OfferImpressionState();
            state.Open(Offer(), T0, GRACE);

            Assert.IsNull(state.Close(Sec(5), GRACE).param.isPurchased);
        }

        [Test]
        public void OpenWhileOpen_ReturnsPreviousParamToLog()
        {
            var state = new OfferImpressionState();
            var first = Offer(PRODUCT);
            state.Open(first, T0, GRACE);

            var (_, replaced) = state.Open(Offer("com.game.other"), Sec(8), GRACE);

            Assert.AreSame(first, replaced.param, "Offer cũ phải được trả ra để log, không bỏ im lặng");
            Assert.AreEqual(8L, replaced.impressionDurationSec);
        }

        // ── Click muộn (bản tin bổ sung — chốt với loader 12/08) ─────────────────

        [Test]
        public void Click_BeforePause_IsJustRecorded()
        {
            // Bản tin impression chưa gửi → cờ isClicked sẽ nằm ngay trên dòng, khỏi bổ sung
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);

            Assert.AreEqual(OfferClick.Recorded, state.MarkClicked().result);
        }

        [Test]
        public void Click_AfterPauseSnapshot_SignalsSupplementNeeded()
        {
            // Dòng impression đã đi lúc pause với isClicked = false — cú bấm sau resume phải được
            // báo ra để service bắn bản tin bổ sung (dòng đã gửi không sửa lại được)
            var state = new OfferImpressionState();
            var (id, _) = state.Open(Offer(PRODUCT), T0, GRACE);
            state.TakePauseSnapshot(Sec(3));

            var click = state.MarkClicked();

            Assert.AreEqual(OfferClick.RecordedAfterLogged, click.result);
            Assert.AreEqual(id, click.offerImpressionId, "Bản tin bổ sung phải mang đúng vé của dòng đã gửi");
        }

        [Test]
        public void Click_Twice_SignalsOnlyOnce()
        {
            // Bấm liên hồi không được đẻ N bản tin bổ sung
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);
            state.TakePauseSnapshot(Sec(3));

            state.MarkClicked();

            Assert.AreEqual(OfferClick.AlreadyClicked, state.MarkClicked().result);
        }

        [Test]
        public void Click_WithoutOpenOffer_IsIgnored()
        {
            Assert.AreEqual(OfferClick.NoOpenOffer, new OfferImpressionState().MarkClicked().result);
        }

        [Test]
        public void Click_DuringGraceWindow_IsIgnored()
        {
            // Offer đã đóng thật (không phải pause) thì không còn gì để bấm — grace chỉ dành cho
            // attribution giao dịch, không hồi sinh click
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);
            state.Close(Sec(5), GRACE);

            Assert.AreEqual(OfferClick.NoOpenOffer, state.MarkClicked().result);
        }

        // ── Pause ────────────────────────────────────────────────────────────────

        [Test]
        public void PauseSnapshot_ReturnsParam_ButKeepsAttributionAlive()
        {
            var state = new OfferImpressionState();
            var (id, _) = state.Open(Offer(PRODUCT), T0, GRACE);

            var snapshot = state.TakePauseSnapshot(Sec(3));

            Assert.IsNotNull(snapshot, "Chốt impression để không mất nếu app bị kill");
            Assert.AreEqual(3L, snapshot.impressionDurationSec);
            Assert.AreEqual(id, state.OpenImpressionId, "State còn sống sau pause");
            Assert.AreEqual(id, state.TryAttribute(PRODUCT, Sec(9)), "Mua sau khi resume vẫn quy được");
        }

        [Test]
        public void PauseSnapshot_LeavesIsPurchasedNull_NotFalse()
        {
            // Impression chưa thực sự kết thúc → chưa được kết luận "không mua",
            // nếu không log impression (false) sẽ mâu thuẫn với log mua mang cùng id.
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);

            Assert.IsNull(state.TakePauseSnapshot(Sec(3)).param.isPurchased);
        }

        [Test]
        public void CloseAfterPauseSnapshot_DoesNotLogTwice()
        {
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);
            state.TakePauseSnapshot(Sec(3));

            Assert.IsNull(state.Close(Sec(9), GRACE), "Đã log lúc pause rồi thì không log lại");
            Assert.IsNull(state.OpenImpressionId, "Nhưng state mở phải được dọn");
        }

        [Test]
        public void PauseSnapshot_Twice_OnlyFirstReturnsParam()
        {
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);

            Assert.IsNotNull(state.TakePauseSnapshot(Sec(3)));
            Assert.IsNull(state.TakePauseSnapshot(Sec(6)));
        }

        [Test]
        public void PauseSnapshot_WithoutOpenOffer_ReturnsNull()
        {
            Assert.IsNull(new OfferImpressionState().TakePauseSnapshot(T0));
        }

        // ── Cửa sổ ân hạn sau khi đóng ───────────────────────────────────────────

        [Test]
        public void PurchaseAfterClose_WithinGrace_IsAttributed()
        {
            // Luồng: bấm mua → popup đóng/forward sang shop → callback giao dịch mới về
            var state = new OfferImpressionState();
            var (id, _) = state.Open(Offer(PRODUCT), T0, GRACE);
            state.MarkClicked();
            state.Close(Sec(5), GRACE);

            Assert.AreEqual(id, state.TryAttribute(PRODUCT, Sec(60)));
        }

        [Test]
        public void PurchaseAfterClose_BeyondGrace_IsNotAttributed()
        {
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);
            state.Close(Sec(5), GRACE);

            Assert.IsNull(state.TryAttribute(PRODUCT, Sec(5) + GRACE + 1));
        }

        [Test]
        public void PurchaseAfterClose_ZeroGrace_IsNotAttributed()
        {
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, 0);
            state.Close(Sec(5), 0);

            Assert.IsNull(state.TryAttribute(PRODUCT, Sec(5)));
        }

        [Test]
        public void OpenOfferWins_OverGraceOffer_WhenBothMatch()
        {
            // Offer A đóng rồi forward sang shop (cũng là offer surface) — mua sản phẩm cả hai
            // cùng chào thì tính cho offer ĐANG hiển thị, không mập mờ.
            var state = new OfferImpressionState();
            state.Open(Offer(PRODUCT), T0, GRACE);
            state.Close(Sec(5), GRACE);

            var (shopId, _) = state.Open(Offer(PRODUCT), Sec(6), GRACE);

            Assert.AreEqual(shopId, state.TryAttribute(PRODUCT, Sec(20)));
        }

        [Test]
        public void GraceOffer_StillMatches_WhenOpenOfferHasDifferentProducts()
        {
            // Offer A (gem) → forward sang shop chào sản phẩm khác → user vẫn mua gem của A
            var state = new OfferImpressionState();
            var (firstId, _) = state.Open(Offer(PRODUCT), T0, GRACE);
            state.Close(Sec(5), GRACE);
            state.Open(Offer("com.game.other_pack"), Sec(6), GRACE);

            Assert.AreEqual(firstId, state.TryAttribute(PRODUCT, Sec(20)));
        }

        // ── extras khai lúc ĐÓNG (OnClosed(extraMeta)) ──────────────────────────

        [Test]
        public void MergeExtraMeta_CloseTimeExtras_WinOverShownTime()
        {
            var state = new OfferImpressionState();
            var param = Offer(PRODUCT);
            param.extraMeta = new System.Collections.Generic.Dictionary<string, object>
            {
                ["close_reason"] = "unknown", ["shelf_slot"] = 2
            };
            state.Open(param, T0, GRACE);

            state.MergeExtraMeta(new System.Collections.Generic.Dictionary<string, object>
            {
                ["close_reason"] = "dismissed"
            });
            var record = state.Close(Sec(5), GRACE);

            Assert.AreEqual("dismissed", record.param.extraMeta["close_reason"], "Tin lúc đóng thắng");
            Assert.AreEqual(2, record.param.extraMeta["shelf_slot"], "Key không trùng giữ nguyên");
        }

        [Test]
        public void MergeExtraMeta_WithoutOpenOffer_IsIgnored()
        {
            var state = new OfferImpressionState();
            // Không có offer đang mở — không được ném, không được tạo state ma
            state.MergeExtraMeta(new System.Collections.Generic.Dictionary<string, object> { ["x"] = 1 });

            Assert.IsNull(state.OpenImpressionId);
        }
    }
}
