using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// Test CHUYỂN PHIÊN qua đúng tầng đĩa (serialize + IDataPool), không chỉ tầng state:
    /// "phiên 2" = dựng service/cache MỚI TINH nhưng đưa cùng một <see cref="FakeDataPool"/> —
    /// pool đóng vai cái đĩa sống sót qua kill. Trước file này, các đường JSON round-trip dưới đây
    /// chỉ được kiểm bằng tay.
    /// </summary>
    public class CrossSessionPersistenceTests
    {
        // ── Sổ transactionId PENDING (bẫy #4 §D4) ───────────────────────────────

        [Test]
        public void PendingTxn_SurvivesRestart_AndConsumesExactlyOnce()
        {
            var disk = new FakeDataPool();

            // Phiên 1: Google trả PENDING, module IAP báo kèm transactionId
            new PurchaseAttemptCache(disk).RegisterPendingTransaction("txn-123");
            Assert.GreaterOrEqual(disk.SyncCount, 1,
                "Ghi sổ PENDING phải ĐẨY XUỐNG ĐĨA ngay — dataPool tự sync 5 phút/lần là quá thưa " +
                "cho dữ liệu sống nhiều ngày (FDataPool chỉ ghi RAM trong Compute)");

            // Phiên 2 (vài ngày sau): giao dịch hoàn tất — phải nhận ra là pending cũ, đúng MỘT lần
            var session2 = new PurchaseAttemptCache(disk);
            Assert.IsTrue(session2.TryConsumePendingTransaction("txn-123"),
                "PENDING hoàn tất ở phiên sau phải tra được sổ — sổ chết theo process là gán nhầm lượt mới");
            Assert.IsFalse(session2.TryConsumePendingTransaction("txn-123"), "Tiêu rồi phải xoá sổ");
            Assert.IsFalse(new PurchaseAttemptCache(disk).TryConsumePendingTransaction("txn-123"),
                "Xoá sổ phải xuống tận đĩa, không chỉ RAM");
        }

        [Test]
        public void PendingTxn_UnknownTransaction_IsNotConsumed()
        {
            var disk = new FakeDataPool();
            new PurchaseAttemptCache(disk).RegisterPendingTransaction("txn-123");

            Assert.IsFalse(new PurchaseAttemptCache(disk).TryConsumePendingTransaction("txn-999"),
                "Giao dịch thường không được ăn nhầm sổ pending");
        }

        [Test]
        public void PendingTxn_FifoCap_DropsTheOldest()
        {
            var disk = new FakeDataPool();
            var cache = new PurchaseAttemptCache(disk);
            for (var i = 0; i < 40; i++) cache.RegisterPendingTransaction("txn-" + i);

            var session2 = new PurchaseAttemptCache(disk);
            Assert.IsFalse(session2.TryConsumePendingTransaction("txn-0"), "Quá trần 32 thì cái cũ nhất rơi");
            Assert.IsTrue(session2.TryConsumePendingTransaction("txn-39"), "Cái mới nhất phải còn");
        }

        // ── Nhãn VẬT (§D11) — JSON round-trip per entity ───────────────────────

        [Test]
        public void EntityLabels_SurviveRestart_PerEntityAndPerKind()
        {
            var disk = new FakeDataPool();

            var session1 = new EntityLabelService(disk);
            session1.Set(LabelEntityKind.Level, "42", "cluster", "hard_1");
            session1.Set(LabelEntityKind.Offer, "42", "theme", "bf2026"); // cùng id "42", khác KIND

            var session2 = new EntityLabelService(disk);
            Assert.AreEqual("hard_1", session2.BundleFor(LabelEntityKind.Level, "42")["cluster"]);
            Assert.AreEqual("bf2026", session2.BundleFor(LabelEntityKind.Offer, "42")["theme"]);
            Assert.IsFalse(session2.BundleFor(LabelEntityKind.Level, "42").ContainsKey("theme"),
                "Level \"42\" và Offer \"42\" phải là hai kho khác nhau — kind nằm trong khoá lưu");
        }

        [Test]
        public void EntityLabels_RemovalPersists_KeyDoesNotResurrect()
        {
            var disk = new FakeDataPool();

            var session1 = new EntityLabelService(disk);
            session1.Set(LabelEntityKind.Level, "42", "cluster", "hard_1");
            session1.Set(LabelEntityKind.Level, "42", "cluster", null); // GỠ

            Assert.IsNull(new EntityLabelService(disk).BundleFor(LabelEntityKind.Level, "42"),
                "Nhãn đã gỡ không được sống lại sau restart");
        }

        [Test]
        public void RegisteredLevelConfig_SurvivesRestart()
        {
            // moves_limit/time_limit_sec rời đường flat rồi — kho nhãn là nơi DUY NHẤT giữ chúng,
            // nên sống-qua-restart ở đây là điều kiện để save của game không phải vác hộ nữa.
            // Đi qua LevelTurnService.SetLevelConfig — đúng cửa mà OnLevelStart dùng thật
            // (SetRegistered là internal, và test qua cửa thật thì phủ luôn đoạn nối).
            var disk = new FakeDataPool();

            new LevelTurnService(new EntityLabelService(disk)).SetLevelConfig(42, movesLimit: 30, timeLimitSec: 90);

            var bundle = new EntityLabelService(disk).BundleFor(LabelEntityKind.Level, "42");
            Assert.AreEqual(30L, System.Convert.ToInt64(bundle[FLevelLabelKey.MOVES_LIMIT]));
            Assert.AreEqual(90L, System.Convert.ToInt64(bundle[FLevelLabelKey.TIME_LIMIT_SEC]));
        }

        // ── Common param (persist opt-in) — JSON round-trip ────────────────────

        [Test]
        public void CommonParam_PersistOptIn_SurvivesRestart()
        {
            var disk = new FakeDataPool();

            var session1 = new CommonParamRepository(disk);
            session1.Set("guild_id", "g_1207", persist: true);
            session1.Set("battle_pass_tier", 3, persist: false);

            var session2 = new CommonParamRepository(disk);
            var info = session2.GetInfo();
            Assert.AreEqual("g_1207", info["guild_id"], "Key bật persist phải có mặt từ log ĐẦU phiên sau");
            Assert.IsFalse(info.ContainsKey("battle_pass_tier"),
                "Không bật persist thì chết theo phiên — giá trị phiên trước chưa chắc còn đúng");
        }

        [Test]
        public void CommonParam_FreshValueInSession2_BeatsThePersistedOne()
        {
            var disk = new FakeDataPool();
            new CommonParamRepository(disk).Set("guild_id", "g_OLD", persist: true);

            var session2 = new CommonParamRepository(disk);
            session2.Set("guild_id", "g_NEW", persist: true); // game khai lại sớm trong phiên 2

            Assert.AreEqual("g_NEW", session2.GetInfo()["guild_id"],
                "Giá trị phiên NÀY không được bị bản đã lưu đè — mới đúng hơn cũ");
        }

        [Test]
        public void CommonParam_RemovalPersists()
        {
            var disk = new FakeDataPool();
            new CommonParamRepository(disk).Set("guild_id", "g_1207", persist: true);

            var session2 = new CommonParamRepository(disk);
            session2.Remove("guild_id"); // người chơi rời guild

            Assert.IsFalse(new CommonParamRepository(disk).GetInfo().ContainsKey("guild_id"),
                "Đã gỡ thì phiên 3 không được thấy lại");
        }
    }
}
