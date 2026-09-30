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
    /// Tham số của log MUA THÀNH CÔNG — mốc cuối của phễu checkout. Kế thừa
    /// <see cref="IapPurchaseAttemptParam"/> nên <c>productId</c> / <c>where</c> /
    /// <c>localizedPrice</c> / <c>isoCurrencyCode</c> chỉ khai MỘT chỗ cho cả họ.
    /// <br/>Khác mốc mở/thất bại ở chỗ <c>where</c> và <c>isoCurrencyCode</c> là BẮT BUỘC (ràng
    /// buộc đặt ở <see cref="CorrectValues"/> của riêng lớp này): một giao dịch có tiền thật mà
    /// không biết tiền tệ nào thì không quy ra USD được.
    /// </summary>
    [Serializable]
    public class InAppParam : IapPurchaseAttemptParam
    {
        public InAppParam()
        {
            where = UNKNOWN;
            isoCurrencyCode = UNKNOWN;
        }

        [NotNull] public string iapWhen = UNKNOWN;
        [NotNull] public string transactionId = UNKNOWN;

        /// <summary>
        ///     Con dấu verdict của validator — <see cref="FIapValidationStatus"/> (phương án B
        ///     20/08): log mua bắn ở MỌI nhánh verdict kèm dấu này thay vì bị chặn sau validator.
        ///     Reject vẫn là LOG MUA (store có giao dịch thật, status nói nó không được công nhận);
        ///     validator chết → <c>unvalidated</c>, doanh thu claim không tụt theo uptime;
        ///     game không có validator → <c>none</c>. Vắng mặt = bản SDK/module cũ chưa migrate.
        /// </summary>
        [FKey(RemoveIfNull = true)] [CanBeNull] public string validationStatus;

        [FKey(RemoveIfNull = true)] [CanBeNull]
        public string purchaseToken;
        [FKey(RemoveIfNull = true)] [CanBeNull]
        public string purchaseMethod;
        [FKey(RemoveIfNull = true)] public int? currentLevel;

        public override void CorrectValues()
        {
            base.CorrectValues();

            // localizedPrice thành nullable từ lúc gộp vào họ IapPurchaseAttemptParam — mốc MỞ và
            // mốc THẤT BẠI có quyền không biết giá. Mốc mua XONG thì không: trước khi gộp, kiểu
            // decimal đặc khiến field này LUÔN có mặt trên f_sdk_in_app_data. Để nó vắng được là
            // âm thầm đổi hợp đồng của một field DOANH THU, nên chặn ở đây — cùng khuôn where/
            // isoCurrencyCode ở dưới, và giữ đúng giá trị cũ (0) cho ca game không điền.
            if (localizedPrice == null)
            {
                CheckNonNull(null, nameof(localizedPrice));
                localizedPrice = 0m;
            }

            where = CheckNonBlank(where, nameof(where));
            isoCurrencyCode = CheckNonBlank(isoCurrencyCode, nameof(isoCurrencyCode));
            iapWhen = CheckNonBlank(iapWhen, nameof(iapWhen));
            transactionId = CheckNonBlank(transactionId, nameof(transactionId));

            currentLevel = CheckNumberNonNegative(currentLevel, nameof(currentLevel));
        }
    }

    [Serializable]
    public class OldCodeSupportInAppParam : InAppParam
    {
        // extraMeta hoisted lên IapPurchaseAttemptParam (base cả phễu checkout) khi Iap.OnStarted
        // mở tham số này — con không khai lại (bài học FKeyService hai field cùng tên)
    }
}