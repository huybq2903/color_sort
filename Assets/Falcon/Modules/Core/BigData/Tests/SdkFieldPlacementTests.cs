using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// Không dựng log thật ở đây (constructor kéo <c>ITimeRepository</c> ra khỏi container DI) —
    /// dùng reflection đúng như <c>FKeyService.Encode</c> vẫn làm khi dựng bản tin.
    /// <br/>Hai luật được canh:
    /// <br/>1. <b>param = cái GAME khai, log = cái SDK thêm.</b> Field SDK tự sinh/tự đo mà nằm
    /// trong param thì game set đè được, và ba tầng (state / service / decorator) sẽ giành nhau
    /// ghi vào một chỗ.
    /// <br/>2. <b>Không khai trùng tên field giữa lớp cha và lớp con.</b> <c>GetFields</c> trả CẢ
    /// hai, thứ tự không xác định, nên bản tin lấy phải cái nào là hên xui — lỗi đã dính hai lần
    /// (<c>purchaseAttemptId</c>, <c>extraMeta</c>).
    /// </summary>
    public class SdkFieldPlacementTests
    {
        private const BindingFlags WIRE = BindingFlags.Public | BindingFlags.Instance;

        private static void AssertOnLogNotOnParam(Type logType, Type paramType, params string[] fields)
        {
            foreach (var field in fields)
            {
                Assert.IsNotNull(logType.GetField(field, WIRE),
                    $"{logType.Name}.{field} biến mất — bản tin mất hẳn field này");
                Assert.IsNull(paramType.GetField(field, WIRE),
                    $"{paramType.Name}.{field} là thứ SDK tự điền, không được để game set đè");
            }
        }

        [Test]
        public void AdViewLifecycle_SdkFieldsLiveOnTheLog()
        {
            AssertOnLogNotOnParam(typeof(FAdRequestLog), typeof(AdViewParam), "adViewId");
            AssertOnLogNotOnParam(typeof(FAdShowLog), typeof(AdShowParam), "adViewId", "requestToShowMs");
            AssertOnLogNotOnParam(typeof(FAdCloseLog), typeof(AdCloseParam), "adViewId", "shownDurationSec");
            AssertOnLogNotOnParam(typeof(FAdLog), typeof(AdParam), "adViewId", "fillLatencyMs", "impressionCount");
        }

        [Test]
        public void Checkout_SdkFieldsLiveOnTheLog()
        {
            AssertOnLogNotOnParam(typeof(FIapStartPurchaseLog), typeof(IapPurchaseAttemptParam), "purchaseAttemptId");
            AssertOnLogNotOnParam(typeof(FIapPurchaseFailLog), typeof(IapPurchaseFailParam), "purchaseAttemptId");
            AssertOnLogNotOnParam(typeof(FInAppLog), typeof(InAppParam), "purchaseAttemptId", "offerImpressionId");
            AssertOnLogNotOnParam(typeof(FIapOfferLog), typeof(IapOfferParam), "offerImpressionId", "impressionDuration");
        }

        [Test]
        public void OfferDuration_StaysInSecondsOnTheWire()
        {
            // Trước đây IapOfferParam.ToDictionary() ép TimeSpan về (long)TotalSeconds. Dời lên log
            // mà đổi kiểu/đơn vị là đổi hợp đồng dù tên key không đổi.
            Assert.AreEqual(typeof(long), typeof(FIapOfferLog).GetField("impressionDuration", WIRE).FieldType);
        }

        [Test]
        public void ForeignSubjectAttributes_LiveInTheirOwnContainer()
        {
            // §H6: attribute của chủ thể NGOẠI đi trong container mang tên chủ thể, không thả flat.
            // elo tả NGƯỜI CHƠI (nay đi f_sdk_user_label), moves_limit/time_limit_sec tả BẢN THIẾT
            // KẾ MÀN (đi bundle levelLabels).
            foreach (var name in new[] { "elo", "movesLimit", "timeLimitSec" })
                Assert.IsNull(typeof(LevelPassParamV2).GetField(name, WIRE),
                    $"'{name}' quay lại đường flat của level event — sai chủ thể");

            Assert.IsNotNull(typeof(FLevelLog).GetField("levelLabels", WIRE));
            Assert.IsNotNull(typeof(FIapOfferLog).GetField("offerLabels", WIRE),
                "Nhãn của BẢN THIẾT KẾ offer (khoá offerId), khác f_sdk_iap_offer_label vốn dán cho một lần hiển thị");
        }

        [Test]
        public void NoFieldNameIsDeclaredTwiceInAnyPayloadHierarchy()
        {
            var payloadTypes = typeof(FParam).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && (typeof(FParam).IsAssignableFrom(t) || typeof(PlainLog).IsAssignableFrom(t)));

            var offenders = new List<string>();
            foreach (var type in payloadTypes)
            {
                var seen = new Dictionary<string, Type>();
                for (var t = type; t != null; t = t.BaseType)
                foreach (var field in t.GetFields(WIRE | BindingFlags.DeclaredOnly))
                {
                    if (seen.TryGetValue(field.Name, out var owner))
                        offenders.Add($"{type.Name}: '{field.Name}' khai ở cả {owner.Name} và {t.Name}");
                    else seen[field.Name] = t;
                }
            }

            Assert.IsEmpty(offenders, string.Join("\n", offenders));
        }
    }
}
