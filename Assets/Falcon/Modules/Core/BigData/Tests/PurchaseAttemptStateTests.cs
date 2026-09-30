using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class PurchaseAttemptStateTests
    {
        private const string PRODUCT = "com.game.gem_pack_1";

        private static IapPurchaseAttemptParam Attempt(string productId = PRODUCT, string where = "shop")
        {
            return new IapPurchaseAttemptParam { productId = productId, where = where };
        }

        [Test]
        public void Open_GeneratesAttemptId()
        {
            var state = new PurchaseAttemptState();

            var (attemptId, replaced) = state.Open(Attempt());

            Assert.IsFalse(replaced, "Không có lượt nào trước đó");
            Assert.IsNotNull(attemptId);
            Assert.AreEqual(attemptId, state.OpenAttemptId);
        }

        [Test]
        public void OpenWhileOpen_ReportsReplacement()
        {
            var state = new PurchaseAttemptState();
            state.Open(Attempt());

            Assert.IsTrue(state.Open(Attempt("com.game.other")).replaced, "Lượt cũ chưa kết thúc bị đè");
        }

        [Test]
        public void TakeFail_CarriesSameIdAndContext_ThenCloses()
        {
            var state = new PurchaseAttemptState();
            var (openedId, _) = state.Open(Attempt());

            var fail = state.TakeFail(IapPurchaseFailReason.UserCanceled);

            Assert.IsNotNull(fail.param);
            Assert.AreEqual(openedId, fail.attemptId, "Phễu khớp bằng cùng một id");
            Assert.AreEqual(PRODUCT, fail.param.productId);
            Assert.AreEqual("shop", fail.param.where);
            Assert.AreEqual(IapPurchaseFailReason.UserCanceled, fail.param.failReason);
            Assert.IsNull(state.OpenAttemptId, "Fail là terminal → lượt đóng lại");
        }

        [Test]
        public void TakeFail_WithoutOpenAttempt_ReturnsNull()
        {
            // App bị kill giữa dialog rồi callback mới về ở phiên sau: state đã chết cùng process.
            // Không bịa ra lượt mua không tồn tại — server đã thấy "start cụt đuôi".
            Assert.IsNull(new PurchaseAttemptState().TakeFail(IapPurchaseFailReason.Network).param);
        }

        [Test]
        public void TryAttributeSuccess_MatchingProduct_ReturnsIdAndCloses()
        {
            var state = new PurchaseAttemptState();
            var (openedId, _) = state.Open(Attempt());

            Assert.AreEqual(openedId, state.TryAttributeSuccess(PRODUCT));
            Assert.IsNull(state.OpenAttemptId, "Mua thành công là terminal → lượt đóng lại");
        }

        [Test]
        public void TryAttributeSuccess_OtherProduct_ReturnsNullAndKeepsAttempt()
        {
            var state = new PurchaseAttemptState();
            state.Open(Attempt());

            Assert.IsNull(state.TryAttributeSuccess("com.game.something_else"));
            Assert.IsNotNull(state.OpenAttemptId, "Lượt mua sản phẩm khác không đóng lượt đang chạy");
        }

        [Test]
        public void TryAttributeSuccess_WithoutOpenAttempt_ReturnsNull()
        {
            Assert.IsNull(new PurchaseAttemptState().TryAttributeSuccess(PRODUCT));
        }

        [Test]
        public void SecondTerminal_AfterClose_ReturnsNothing()
        {
            // Callback về hai lần (fail rồi success, hoặc success lặp) không được sinh id ma
            var state = new PurchaseAttemptState();
            state.Open(Attempt());
            state.TryAttributeSuccess(PRODUCT);

            Assert.IsNull(state.TakeFail(IapPurchaseFailReason.Network).param);
            Assert.IsNull(state.TryAttributeSuccess(PRODUCT));
        }

        [Test]
        public void PendingFail_ClosesAttempt_LaterPurchaseNotAttributed()
        {
            // Google PENDING: log fail(reason=pending) rồi giao dịch hoàn tất sau (có thể vài ngày).
            // Log mua lúc đó KHÔNG mang attempt id — server nối bằng phễu theo product/user/time.
            var state = new PurchaseAttemptState();
            state.Open(Attempt());

            var fail = state.TakeFail(IapPurchaseFailReason.Pending);
            Assert.AreEqual(IapPurchaseFailReason.Pending, fail.param.failReason);
            Assert.IsNull(state.TryAttributeSuccess(PRODUCT));
        }

        [Test]
        public void FailParam_SerializesReason_ButNotTheSdkId()
        {
            var state = new PurchaseAttemptState();
            state.Open(Attempt());
            var dict = state.TakeFail(IapPurchaseFailReason.ItemUnavailable).param.ToDictionary();

            Assert.AreEqual(IapPurchaseFailReason.ItemUnavailable, dict["failReason"]);
            Assert.IsFalse(dict.ContainsKey("purchaseAttemptId"), "Id của SDK nằm trên log, không ở param");
            Assert.AreEqual(PRODUCT, dict["productId"]);
        }

        [Test]
        public void AttemptParam_OmitsNullWhere()
        {
            var param = new IapPurchaseAttemptParam { productId = PRODUCT };
            Assert.IsFalse(param.ToDictionary().ContainsKey("where"), "null thì vắng mặt khỏi payload (§H4)");
        }
    }
}
