/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Phần chung của mọi mốc trong vòng đời MỘT LẦN XEM AD: request → show → impression → close
    /// (xem EntityLifecycle-Design.md §4f). Cả 4 mốc mang cùng <c>adViewId</c> — id đó do SDK sinh nên nằm trên log (<see cref="AAdLog"/>), không nằm ở đây.
    /// </summary>
    [Serializable]
    public class AdViewParam : FParam
    {
        public AdType type;

        /// <summary>
        /// Vị trí/ngữ cảnh — cùng tên với <see cref="AdParam.adWhere"/> để join dễ.
        /// Lúc request thường chưa biết (preload); lúc show thì biết.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull] public string adWhere;

        /// <summary>
        /// Ngữ cảnh/thời điểm kích hoạt — cùng tên với <see cref="AdParam.adWhen"/> để join dễ.
        /// Như <see cref="adWhere"/>: preload thường chưa biết, lúc show mới biết.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull] public string adWhen;

        /// <summary>Mediation đang dùng (Max, IronSource, AdMob...) nếu biết.</summary>
        [FKey(RemoveIfNull = true)] [CanBeNull] public string adMediation;

        /// <summary>
        /// Loại kết nối lúc xin ad — SDK-core tự đo. Giải thích fill-fail: không có mạng thì
        /// mediation không trả ad được. Chỉ điền ở mốc request (show/close không cần).
        /// </summary>
        [FKey(RemoveIfNull = true)] public NetworkType? networkType;

        /// <summary>
        /// Key-value tuỳ ý của mediation/game cho MỐC này (waterfall, adUnitId...) — flatten vào
        /// payload, server lưu <c>event_extra_props</c>. Cột hợp đồng luôn thắng khi trùng key.
        /// Khai ở base nên cả ba mốc request/show/close đều dùng được.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> extraMeta;

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }
    }

    /// <summary>Mốc gọi hiển thị ad (§4f).</summary>
    [Serializable]
    public class AdShowParam : AdViewParam
    {
    }

    /// <summary>Mốc ad đóng lại, người chơi quay về game (§4f).</summary>
    [Serializable]
    public class AdCloseParam : AdViewParam
    {

        /// <summary>Người chơi có bấm vào ad không (mediation điền nếu biết).</summary>
        [FKey(RemoveIfNull = true)] public bool? hasClick;

        /// <summary>
        /// Xem hết ad hay bỏ giữa chừng (rewarded: có được trả thưởng không) — mediation điền.
        /// Đây là <c>ad_completed</c> của hợp đồng §D2, đặt ở mốc close vì chỉ lúc đóng mới biết.
        /// </summary>
        [FKey(RemoveIfNull = true)] public bool? adCompleted;
    }
}
