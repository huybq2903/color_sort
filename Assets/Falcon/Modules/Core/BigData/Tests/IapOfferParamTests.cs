using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    public class IapOfferParamTests
    {
        [Test]
        public void ContractD3Fields_AbsentWhenNull_PresentWhenSet()
        {
            // §H4: field mới không nhập thì VẮNG MẶT khỏi payload, không clamp/điền default
            var empty = new IapOfferParam().ToDictionary();
            Assert.IsFalse(empty.ContainsKey("offerPrices"));
            Assert.IsFalse(empty.ContainsKey("offerDiscounts"));
            Assert.IsFalse(empty.ContainsKey("offerCurrencyCode"));
            Assert.IsFalse(empty.ContainsKey("isPurchased"));

            var param = new IapOfferParam
            {
                offerProductId = new[] { "gem_100" },
                offerPrices = new Dictionary<string, double> { ["gem_100"] = 129000 },
                offerDiscounts = new Dictionary<string, float> { ["gem_100"] = 0.3f },
                offerCurrencyCode = "VND",
                isPurchased = true
            };
            var dict = param.ToDictionary();
            Assert.AreEqual(129000d, ((Dictionary<string, double>)dict["offerPrices"])["gem_100"]);
            Assert.AreEqual(0.3f, ((Dictionary<string, float>)dict["offerDiscounts"])["gem_100"]);
            Assert.AreEqual("VND", dict["offerCurrencyCode"]);
            Assert.AreEqual(true, dict["isPurchased"]);
        }

        [Test]
        public void PerProductMaps_CarryEachProductSeparately()
        {
            // Câu của game team: một offer, mỗi product một mức giảm. Một số vô dụng ở đây —
            // loader chốt 12/08 dùng map, và đây là fact của LẦN HIỂN THỊ (giá đổi theo đợt sale)
            // chứ không phải nhãn thiết kế.
            var param = new IapOfferParam
            {
                offerProductId = new[] { "gem_100", "gem_500" },
                offerPrices = new Dictionary<string, double> { ["gem_100"] = 29000, ["gem_500"] = 129000 },
                offerDiscounts = new Dictionary<string, float> { ["gem_100"] = 0.3f, ["gem_500"] = 0.5f },
                offerCurrencyCode = "VND"
            };

            var discounts = (Dictionary<string, float>)param.ToDictionary()["offerDiscounts"];
            Assert.AreEqual(0.3f, discounts["gem_100"]);
            Assert.AreEqual(0.5f, discounts["gem_500"]);
            Assert.AreEqual("VND", param.ToDictionary()["offerCurrencyCode"],
                "Tiền tệ vẫn là MỘT giá trị — cả kệ bán cùng loại tiền");
        }

        [Test]
        public void EmptyMaps_VanishInsteadOfSendingBraces()
        {
            // §H4: không có thì VẮNG MẶT. RemoveIfNull chỉ bắt null nên map rỗng phải hạ về null
            // Điền nốt field bắt buộc để CorrectValues không log lỗi về chuyện khác
            var param = new IapOfferParam
            {
                offerId = "black_friday", offerCategory = "gem_bundle", offerLayout = "discount",
                placementScene = "shop", triggerEvent = "on_login",
                offerPrices = new Dictionary<string, double>(),
                offerDiscounts = new Dictionary<string, float>()
            };

            // CorrectValues() là chỗ chuẩn hoá, và constructor của log gọi nó trước khi bản tin
            // đi — nên đây đúng thứ tự thật, không phải mẹo của test
            param.CorrectValues();
            var dict = param.ToDictionary();

            Assert.IsFalse(dict.ContainsKey("offerPrices"));
            Assert.IsFalse(dict.ContainsKey("offerDiscounts"));
        }

        [Test]
        public void PurchasePrice_NeverVanishesFromTheWire()
        {
            // Trước khi gộp họ param, localizedPrice là decimal ĐẶC nên luôn có mặt trên
            // f_sdk_in_app_data. Gộp xong nó thành decimal? + RemoveIfNull ⇒ vắng được, tức âm
            // thầm đổi hợp đồng của một field DOANH THU. CorrectValues phải chặn.
            var param = new InAppParam { productId = "gem_100", transactionId = "tx-1" };

            LogAssert.Expect(LogType.Error, new Regex("localizedPrice"));
            param.CorrectValues();

            Assert.AreEqual(0m, param.localizedPrice);
            Assert.IsTrue(param.ToDictionary().ContainsKey("localizedPrice"));
        }

        [Test]
        public void SdkMeasuredFields_AreNotOnTheParam()
        {
            // param = cái GAME khai; id lần hiển thị và thời lượng là SDK tự đo nên nằm trên log
            var dict = new IapOfferParam().ToDictionary();

            Assert.IsFalse(dict.ContainsKey("offerImpressionId"));
            Assert.IsFalse(dict.ContainsKey("impressionDuration"));
        }
    }
}
