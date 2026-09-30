/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-09-23
 */

using System;
using Falcon.Helpers.Devkit;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// GAME MUỐN CHIẾU AD — mốc đầu của phía CẦU, bắn TRƯỚC khi biết kho có ad hay không.
    /// <br/>Vì sao cần (chốt với loader 23/09): từ bản mediation có van, một đợt xin ad
    /// (<c>f_sdk_ad_request_data</c>) nạp sẵn hàng rồi phục vụ NHIỀU lần chiếu, nên <c>request</c>
    /// là "một đợt nạp hàng", không còn là "một lần user cần ad". Trước mốc này, ca "muốn chiếu
    /// mà kho rỗng" KHÔNG có dòng nào trên wire — mẫu số của fill rate bị mất hẳn.
    /// <br/>Chỉ số đọc ra: <c>view_fill_rate = count(adAvailable = true) / count(*)</c> per
    /// <c>adWhere</c> — "user cần ad thì bao nhiêu lần có ad".
    /// <br/>Bất biến kiểm được: số dòng này ≥ số <c>f_sdk_ad_show_data</c>. Lệch là có luồng show
    /// quên gọi mốc này.
    /// <br/>Vì sao KHÔNG làm event "không-có-ad" (chỉ bắn lúc kho rỗng): mốc chỉ-ghi-khi-hỏng mà
    /// quên gọi một luồng thì phần hỏng biến mất im lặng và fill rate đẹp lên giả tạo, không có
    /// bất biến nào soi được (bài học <c>OnEligible</c> mồ côi của Master League).
    /// </summary>
    [Serializable]
    public class FAdShowAttemptLog : AFalconLog
    {
        public AdType type;

        /// <summary>Chỗ game định chiếu — có cả ở ca không có ad, nên đọc được "chỗ nào hay hụt ad".</summary>
        [FKey(RemoveIfNull = true)] public string adWhere;

        /// <summary>Ngữ cảnh kích hoạt (bỏ trống nếu không biết).</summary>
        [FKey(RemoveIfNull = true)] public string adWhen;

        [FKey(RemoveIfNull = true)] public string adMediation;

        /// <summary>Lúc game hỏi thì kho có ad sẵn không — mediation truyền vào, SDK không đoán hộ.</summary>
        public bool adAvailable;

        /// <summary>
        /// View sắp được chiếu — chỉ có khi <see cref="adAvailable"/>; kho rỗng thì vắng mặt.
        /// SDK tự đọc từ cache, mediation không phải truyền.
        /// </summary>
        [FKey(RemoveIfNull = true)] public string adViewId;

        [Preserve]
        public FAdShowAttemptLog()
        {
        }

        public override string Event => "f_sdk_ad_show_attempt";
    }
}
