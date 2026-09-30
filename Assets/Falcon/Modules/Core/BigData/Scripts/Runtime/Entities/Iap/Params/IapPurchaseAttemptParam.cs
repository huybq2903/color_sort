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
    /// Tham số của một LƯỢT MUA (từ lúc gọi billing flow tới lúc thành công/thất bại) —
    /// hợp đồng §D4. Dùng chung cho log mở lượt và log thất bại.
    /// </summary>
    [Serializable]
    public class IapPurchaseAttemptParam : FParam
    {
        [NotNull] public string productId = UNKNOWN;

        /// <summary>Vị trí/ngữ cảnh kích hoạt mua — dùng chung tên với <see cref="InAppParam.where"/>.</summary>
        [FKey(RemoveIfNull = true)] [CanBeNull] public string where;

        /// <summary>
        /// Giá nội địa hoá của sản phẩm (module IAP điền lúc mở billing flow) — để đo được
        /// GIÁ TRỊ BỊ BỎ GIỎ mà không phải join catalog sản phẩm.
        /// </summary>
        [FKey(RemoveIfNull = true)] public decimal? localizedPrice;

        /// <summary>Mã tiền tệ ISO của <see cref="localizedPrice"/>.</summary>
        [FKey(RemoveIfNull = true)] [CanBeNull] public string isoCurrencyCode;

        /// <summary>
        /// Loại kết nối lúc mốc này xảy ra — SDK-core tự đo. Giải thích được lý do thất bại
        /// kiểu <see cref="IapPurchaseFailReason.Network"/>.
        /// </summary>
        [FKey(RemoveIfNull = true)] public NetworkType? networkType;

        /// <summary>
        /// Key-value tuỳ ý của module IAP/game cho lượt mua này — flatten vào payload, server lưu
        /// <c>event_extra_props</c>; cột hợp đồng thắng khi trùng key. Khai lúc mở lượt
        /// (<c>Iap.OnStarted</c>), log fail của cùng lượt tự mang theo.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> extraMeta;

        public override void CorrectValues()
        {
            productId = CheckNonBlank(productId, nameof(productId));
            // Kiểm ở base để cả ba mốc (mở / thất bại / mua xong) dùng chung một luật giá
            localizedPrice = CheckNumberNonNegative(localizedPrice, nameof(localizedPrice));
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }
    }

    /// <summary>Lượt mua kết thúc bằng thất bại/huỷ/treo — thêm lý do (§D4).</summary>
    [Serializable]
    public class IapPurchaseFailParam : IapPurchaseAttemptParam
    {
        public IapPurchaseFailReason failReason;
    }
}
