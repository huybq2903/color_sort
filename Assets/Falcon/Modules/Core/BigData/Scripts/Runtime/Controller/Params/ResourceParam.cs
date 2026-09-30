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
    [Serializable]
    public class ResourceParam : FParam
    {
        /// <summary>
        ///     Định danh MỘT giao dịch tài nguyên (§D10): "mua 3 booster bằng 500 gold" là hai
        ///     event rời (sink gold + source booster) — cùng exchangeId thì server mới ghép lại
        ///     được để tính giá thật per item, và bundle đa vế (gems → gold + booster + heart)
        ///     mới không vỡ. Dùng <c>FalconBigDataController.Resource.OnExchange</c> để SDK tự sinh + đóng
        ///     dấu; tự set tay cũng được (vd id giao dịch của game server) miễn mọi vế cùng giá trị.
        /// </summary>
        [FKey(RemoveIfNull = true)] public string exchangeId;

        public FlowType flowType;
        [NotNull] public string itemType= UNKNOWN;
        [NotNull] public string currency= UNKNOWN;
        [NotNull] public string itemId= UNKNOWN;
        
        /// <summary>
        ///     VỊ TRÍ phát sinh — màn/panel nào (<c>shop_screen</c>, <c>level_end_popup</c>…).
        ///     Vocab riêng của từng game, SDK không khai hằng số.
        /// </summary>
        [NotNull] public string resourceWhere = UNKNOWN;

        /// <summary>
        ///     NGUYÊN NHÂN phát sinh — vì sao tài nguyên này chảy (<see cref="FResourceWhen"/>).
        ///     Cùng nghĩa "ngữ cảnh kích hoạt" với <see cref="AdViewParam.adWhen"/> /
        ///     <see cref="InAppParam.iapWhen"/>, không phải mốc đồng hồ.
        ///     <br/>⚠ Khác hẳn <c>playTurnId</c> mà SDK đóng tự động: id đó nói "xảy ra TRONG lúc
        ///     đang chơi màn", còn field này nói "xảy ra VÌ". Mua gói trong shop giữa ván vẫn mang
        ///     playTurnId nhưng nguyên nhân là <see cref="FResourceWhen.Shop"/>.
        /// </summary>
        [NotNull] public string resourceWhen = UNKNOWN;
        public long amount;

        [FKey(RemoveIfNull = true)] public int? currentLevel;
        [FKey(RemoveIfNull = true)] public long? valueBefore;
        [FKey(RemoveIfNull = true)] public long? valueAfter;

        /// <summary>
        ///     Nối vế thưởng về LẦN XEM AD đã đẻ ra nó (rewarded ad trả coin...) — loader ký
        ///     18/08. Đóng TẠI CHỖ PHÁT THƯỞNG: lấy <c>FalconBigDataController.Ad.CurrentViewId(type)</c>
        ///     ngay lúc grant reward. SDK CẤM auto-stamp (điều khoản ký): banner luôn "đang mở"
        ///     nên tự đóng là gán nhầm nguyên nhân gần như 100% — ĐỒNG THỜI ≠ NGUYÊN NHÂN.
        ///     Server JOIN sang <c>f_sdk_ads_data</c>.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull] public string adViewId;

        /// <summary>
        ///     Nối vế thưởng về GIAO DỊCH IAP đã đẻ ra nó (mở gói gems...) — cùng phán quyết với
        ///     <see cref="adViewId"/>: đóng tại chỗ phát thưởng (transactionId từ purchase),
        ///     không auto-stamp. Server JOIN sang <c>f_sdk_in_app_data</c>.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull] public string transactionId;
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> detail;

        public override void CorrectValues()
        {
            itemType = CheckNonBlank(itemType, nameof(itemType));
            currency = CheckNonBlank(currency, nameof(currency));
            itemId = CheckNonBlank(itemId, nameof(itemId));
            
            resourceWhere = CheckNonBlank(resourceWhere, nameof(resourceWhere));
            resourceWhen = CheckNonBlank(resourceWhen, nameof(resourceWhen));
            
            currentLevel = CheckNumberNonNegative(currentLevel, nameof(currentLevel));
            amount = CheckNumberNonNegative(amount, nameof(amount));
        }
    }

    [Serializable]
    public class OldCodeSupportResourceParam : ResourceParam
    {
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> extraMeta = null;

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }
    }
}