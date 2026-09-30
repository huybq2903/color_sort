/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-10
 */

using System;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bản tin ad TỔNG HỢP: một dòng mang <c>impressionCount</c> = N impression đã gộp
    /// (xem <see cref="BannerLogService"/>). Cùng event id <c>f_sdk_ads_data</c> với log ad thường
    /// — chỗ khác nhau nằm ở <see cref="IsSpanRecord"/>.
    /// <br/>Vì sao cần một class riêng thay vì một cờ trên param: cờ phải đọc được từ TẦNG LOG
    /// (nơi central params được chèn vào), mà tầng đó không biết gì về <c>AdParam</c>.
    /// <br/>Cái gì gộp được thì gộp (doanh thu, số impression); cái gì là định danh của MỘT lần
    /// xem — <c>adViewId</c>, độ trễ fill, <c>playTurnId</c> — thì để TRỐNG, vì giá trị lúc gửi
    /// chỉ đúng cho impression cuối cùng trong cụm.
    /// </summary>
    [Serializable]
    public class FAggregatedAdLog : FAdLog
    {
        [Preserve]
        public FAggregatedAdLog()
        {
        }

        public FAggregatedAdLog(AdParam param) : base(param)
        {
        }

        public override bool IsSpanRecord => true;
    }
}
