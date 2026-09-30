using System.Reflection;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// Không dựng log thật ở đây: constructor của mọi log kéo <c>ITimeRepository</c> ra khỏi
    /// container DI (field <c>clientCreateDate</c>), nên test edit-mode sẽ phụ thuộc vào việc
    /// container có sống hay không. Mấy bất biến dưới đây kiểm được bằng reflection.
    /// </summary>
    public class AggregatedAdLogTests
    {
        [Test]
        public void SpanRecordFlag_IsAProperty_SoItNeverReachesThePayload()
        {
            // FKeyService.Encode chỉ đọc FIELD public — cờ là property thì không lọt vào bản tin
            Assert.IsNull(typeof(FAggregatedAdLog).GetField("IsSpanRecord"),
                "Là field thì cờ nội bộ sẽ bị gửi lên server");
            Assert.IsNotNull(typeof(FAggregatedAdLog).GetProperty("IsSpanRecord"));
        }

        [Test]
        public void AggregatedLog_OverridesTheFlag()
        {
            var property = typeof(FAggregatedAdLog).GetProperty("IsSpanRecord");

            Assert.AreEqual(typeof(FAggregatedAdLog), property.DeclaringType,
                "Phải tự khai lại, không thì nó dùng mặc định false của PlainLog");
        }

        [Test]
        public void NormalAdLog_KeepsTheDefault()
        {
            var property = typeof(FAdLog).GetProperty("IsSpanRecord");

            Assert.AreEqual(typeof(PlainLog), property.DeclaringType,
                "Log ad thường không được là span-record — nó tả đúng một impression");
        }

        [Test]
        public void AggregatedLog_ReusesTheSameEventId()
        {
            // Gộp hay không thì server vẫn đọc chung một event; khác nhau ở impressionCount
            Assert.IsNull(typeof(FAggregatedAdLog).GetProperty("Event",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                "Khai lại Event là đẻ event id mới cho server phải khai báo thêm");
        }
    }

    public class BannerValueTests
    {
        [Test]
        public void Update_SumsRevenueAndCountsImpressions()
        {
            var value = new BannerValue();
            value.Update(0.001);
            value.Update(0.002);
            value.Update(0.003);

            Assert.AreEqual(0.006, value.AdRev, 0.000001);
            Assert.AreEqual(3, value.ImpressionCount);
        }

        [Test]
        public void FreshValue_StartsEmpty()
        {
            var value = new BannerValue();

            Assert.AreEqual(0, value.AdRev);
            Assert.AreEqual(0, value.ImpressionCount);
        }

        [Test]
        public void ValueHasNoLtv_LedgerOwnsIt()
        {
            // LTV là sổ cái SDK/Mediation tự cộng — nhận từ ngoài vào là claim không bằng chứng
            Assert.IsNull(typeof(BannerValue).GetProperty("AdLtv"));
        }

        // ── adViewId trên dòng gộp (câu F3 của loader, chốt 2026-08-12) ──────────

        [Test]
        public void SameInstanceThroughout_KeepsTheId()
        {
            // Ca phổ biến: banner tự refresh trong CÙNG một instance → dòng gộp vẫn đếm được view thật
            var value = new BannerValue();
            value.Update(0.001, "view-a");
            value.Update(0.002, "view-a");
            value.Update(0.003, "view-a");

            Assert.AreEqual("view-a", value.AdViewId);
        }

        [Test]
        public void ClusterSpanningTwoInstances_DropsTheId()
        {
            // BannerKey không phân biệt instance: banner bị huỷ rồi load lại giữa hai lần chốt vẫn
            // rơi cùng key. Dán id của instance này lên impression của instance kia là bịa.
            var value = new BannerValue();
            value.Update(0.001, "view-a");
            value.Update(0.002, "view-b");

            Assert.IsNull(value.AdViewId);
            Assert.AreEqual(2, value.ImpressionCount, "Vẫn gộp — chỉ id là thứ mất");
        }

        [Test]
        public void OnceMixed_ItStaysMixed()
        {
            // Impression thứ ba trùng id với cái đầu KHÔNG làm cụm sạch trở lại
            var value = new BannerValue();
            value.Update(0.001, "view-a");
            value.Update(0.002, "view-b");
            value.Update(0.003, "view-a");

            Assert.IsNull(value.AdViewId);
        }

        [Test]
        public void NoIdAtAll_IsNotTreatedAsMixed()
        {
            // Mediation không đi qua OnAdRequested → CurrentAdViewId luôn null; đó là "không biết",
            // không phải "vắt qua hai instance"
            var value = new BannerValue();
            value.Update(0.001);
            value.Update(0.002);

            Assert.IsNull(value.AdViewId);
            Assert.AreEqual(2, value.ImpressionCount);
        }
    }
}
