/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */
using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Tham số của mốc IMPRESSION — mốc thứ ba trong vòng đời một lần xem ad. Kế thừa
    /// <see cref="AdViewParam"/> nên <c>type</c> / <c>adWhere</c> / <c>adWhen</c>
    /// / <c>adMediation</c> chỉ khai MỘT chỗ cho cả họ; thêm field mới cho vòng đời thì khỏi phải
    /// nhớ sửa hai nơi.
    /// <br/>Khác ba mốc kia ở chỗ <c>adWhere</c>/<c>adWhen</c> là BẮT BUỘC (ràng buộc đặt ở
    /// <see cref="CorrectValues"/> của riêng lớp này, không đẩy lên cha) — impression mà không
    /// biết chiếu ở đâu thì doanh thu không quy được về chỗ nào.
    /// </summary>
    [Serializable]
    public class AdParam : AdViewParam
    {
        public AdParam()
        {
            // Giữ nếp cũ của mốc impression: chưa nhập thì là "Unknown" chứ không phải vắng mặt
            adWhere = UNKNOWN;
            adWhen = UNKNOWN;
        }

        public double adRev;

        [FKey(RemoveIfNull = true)] public string adPrecision;
        [FKey(RemoveIfNull = true)] public string adCountry;
        [FKey(RemoveIfNull = true)] public string adNetwork;
        [FKey(RemoveIfNull = true)] public int? currentLevel;
        [FKey(RemoveIfNull = true)] public bool? hasClick;
        [FKey(RemoveIfNull = true)] public double? timeShow;

        /// <summary>Tầng waterfall mà ad này thắng (§D2) — mediation adapter điền.</summary>
        [FKey(RemoveIfNull = true)] public int? waterfallPosition;

        /// <summary>
        ///     Ad unit id của mediation. Dữ liệu này đang được gửi Firebase/MMP/game-server nhưng
        ///     chưa từng vào DWH — thiếu nó thì không phân tích được doanh thu theo từng ad unit.
        /// </summary>
        [FKey(RemoveIfNull = true)] public string adUnitId;

        /// <summary>Tên instance của network trong waterfall (IronSource: adInfo.InstanceName).</summary>
        [FKey(RemoveIfNull = true)] public string adInstanceName;

        /// <summary>
        ///     Chuỗi format THÔ do mediation trả về, giữ nguyên không map.
        ///     Lý do: mediation map format → AdType bằng switch có nhánh default (IronSource gán mọi
        ///     format lạ thành Reward) nên AppOpen/Native/MREC bị dán nhãn sai; có chuỗi gốc thì
        ///     server phát hiện được lệch thay vì tin nhầm số liệu.
        /// </summary>
        [FKey(RemoveIfNull = true)] public string adFormatRaw;


        public override void CorrectValues()
        {
            adWhere = CheckNonBlank(adWhere, nameof(adWhere));
            adWhen = CheckNonBlank(adWhen, nameof(adWhen));
            currentLevel = CheckNumberNonNegative(currentLevel, nameof(currentLevel));
            timeShow = CheckNumberNonNegative(timeShow, nameof(timeShow));
            adRev = CheckNumberNonNegative(adRev, nameof(adRev));
        }
    }

    [Serializable]
    public class OldCodeSupportAdParam : AdParam
    {
        // extraMeta hoisted lên AdViewParam (base cả vòng đời ad) khi Ad.OnRequested/Shown/Closed
        // mở tham số này — con không khai lại (bài học FKeyService hai field cùng tên)
    }
}