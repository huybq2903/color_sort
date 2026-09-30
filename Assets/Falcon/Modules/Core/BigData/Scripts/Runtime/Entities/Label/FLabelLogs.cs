/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-11
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bản tin gán nhãn (§D11) — MỘT format, mỗi entity một event (<c>f_sdk_user_label</c>,
    /// <c>f_sdk_session_label</c>, <c>f_sdk_turn_label</c>, <c>f_sdk_level_label</c>).
    /// <br/>Entity nằm ở TÊN EVENT chứ không phải field, theo luật phân xử §D8: vocab entity là
    /// ĐÓNG toàn-fleet nên đưa vào tên event; còn <c>labelKey</c> là vocab MỞ per-game nên ở param.
    /// <br/>Dev KHÔNG phải phân biệt "nhãn" với "tham số động" — một cửa, gửi tuốt; ranh giới đó
    /// là việc của data team.
    /// </summary>
    [Serializable]
    public abstract class ALabelLog : AFalconLog
    {
        public LabelParam param;

        /// <summary>
        /// Id của instance được gán nhãn — SDK tự tra từ vòng đời đang chạy, game không set.
        /// Mỗi log chỉ dùng đúng một cái; khác <c>sessionUid</c>/<c>playTurnId</c> ở chỗ ba id này
        /// KHÔNG được đóng tự động lên mọi log nên bản tin nhãn phải tự mang.
        /// </summary>
        [FKey(RemoveIfNull = true)] public string adViewId;

        /// <inheritdoc cref="adViewId"/>
        [FKey(RemoveIfNull = true)] public string offerImpressionId;

        /// <inheritdoc cref="adViewId"/>
        [FKey(RemoveIfNull = true)] public string purchaseAttemptId;

        [Preserve]
        protected ALabelLog()
        {
        }

        protected ALabelLog(LabelParam param)
        {
            this.param = param;
            AnalyticLogger.Instance.Info($"{Event}:{this.param.ToJson()}");
            this.param.CorrectValues();
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Remove(nameof(param));
            foreach (var (key, value) in param.ToDictionary())
                result.PutIfAbsentAndNotNull(key, value);

            // labelValue = null nghĩa là GỠ NHÃN nên phải lên wire; PutIfAbsentAndNotNull ở trên
            // cố tình bỏ null, nên đóng lại bằng tay.
            result[nameof(param.labelValue)] = param.labelValue;
            return result;
        }
    }

    /// <summary>
    /// Nhãn của NGƯỜI CHƠI — server merge vào bucket <c>user_properties</c> (chung chỗ với
    /// <c>falcon_user_filter</c>, cùng ngữ nghĩa last-wins / gỡ-khi-null) rồi snapshot lên mọi
    /// event SAU đó. Ngữ nghĩa từ-mốc-trở-đi: quá khứ MIỄN NHIỄM.
    /// </summary>
    [Serializable]
    public class FUserLabelLog : ALabelLog
    {
        [Preserve]
        public FUserLabelLog()
        {
        }

        public FUserLabelLog(LabelParam param) : base(param)
        {
        }

        public override string Event => "f_sdk_user_label";
    }

    /// <summary>Nhãn của PHIÊN đang mở — cook nhặt bằng SQL vào hộp session, không qua profile.</summary>
    [Serializable]
    public class FSessionLabelLog : ALabelLog
    {
        [Preserve]
        public FSessionLabelLog()
        {
        }

        public FSessionLabelLog(LabelParam param) : base(param)
        {
        }

        public override string Event => "f_sdk_session_label";
    }

    /// <summary>Nhãn của LƯỢT CHƠI đang mở — cook nhặt vào hộp turn, không qua profile.</summary>
    [Serializable]
    public class FTurnLabelLog : ALabelLog
    {
        [Preserve]
        public FTurnLabelLog()
        {
        }

        public FTurnLabelLog(LabelParam param) : base(param)
        {
        }

        public override string Event => "f_sdk_turn_label";
    }

    /// <summary>
    /// Nhãn của một LẦN XEM AD đang mở — cùng nhóm "instance" với session/turn, cook nhặt bằng SQL
    /// vào hộp ad view.
    /// <br/>Hợp đồng §D11 (bản 11/08) đã chốt event id cho cả ba scope instance; điều kiện nối của
    /// <c>ad_view</c> là <c>adViewId</c> §C phải sống — SDK đã ship nên thoả. Còn chờ loader MỞ
    /// mapping rule thì dữ liệu mới vào hộp.
    /// </summary>
    [Serializable]
    public class FAdViewLabelLog : ALabelLog
    {
        [Preserve]
        public FAdViewLabelLog()
        {
        }

        public FAdViewLabelLog(LabelParam param) : base(param)
        {
        }

        public override string Event => "f_sdk_ad_view_label";
    }

    /// <summary>
    /// Nhãn của một LẦN HIỂN THỊ OFFER đang mở.
    /// <inheritdoc cref="FAdViewLabelLog"/>
    /// </summary>
    [Serializable]
    public class FIapOfferLabelLog : ALabelLog
    {
        [Preserve]
        public FIapOfferLabelLog()
        {
        }

        public FIapOfferLabelLog(LabelParam param) : base(param)
        {
        }

        public override string Event => "f_sdk_iap_offer_label";
    }

    /// <summary>
    /// Nhãn của một LƯỢT MUA đang mở.
    /// <inheritdoc cref="FAdViewLabelLog"/>
    /// </summary>
    [Serializable]
    public class FIapPurchaseLabelLog : ALabelLog
    {
        [Preserve]
        public FIapPurchaseLabelLog()
        {
        }

        public FIapPurchaseLabelLog(LabelParam param) : base(param)
        {
        }

        public override string Event => "f_sdk_iap_purchase_label";
    }
}
