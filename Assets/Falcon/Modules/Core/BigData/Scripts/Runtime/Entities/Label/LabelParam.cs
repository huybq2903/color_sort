/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Một bản tin GÁN NHÃN (§D11) — dùng chung cho cả họ <c>f_sdk_&lt;entity&gt;_label</c>.
    /// Entity nào thì nằm ở TÊN EVENT, không phải ở field.
    /// </summary>
    [Serializable]
    public class LabelParam : FParam
    {
        /// <summary>
        ///     Loại nhãn — TỰ DO, không phải đăng ký trước (registry đã bỏ 2026-08-10, cook nhặt
        ///     theo danh-tính-gốc). Server đưa vào <c>sub_event</c>.
        /// </summary>
        [NotNull] public string labelKey = UNKNOWN;

        /// <summary>
        ///     Giá trị — GIỮ NGUYÊN KIỂU JSON (bool / số / chữ), không ép về string và không nhét
        ///     vào slot typed nào; server để nguyên trong <c>event_extra_props</c>.
        ///     <br/><c>null</c> KHÔNG phải "không có" mà là lệnh **GỠ NHÃN** — nên field này cố ý
        ///     không có <c>RemoveIfNull</c>: null vẫn phải lên wire.
        /// </summary>
        [CanBeNull] public object labelValue;

        /// <summary>
        ///     Id của VẬT được gán nhãn, chỉ dùng cho <c>f_sdk_level_label</c> (§D11: id đi cột tự
        ///     nhiên per kind). Nhãn user/session/turn không cần — id của chúng SDK đã đóng sẵn.
        /// </summary>
        [FKey(RemoveIfNull = true)] public int? currentLevel;

        public override void CorrectValues()
        {
            labelKey = CheckNonBlank(labelKey, nameof(labelKey));
            currentLevel = CheckNumberNonNegative(currentLevel, nameof(currentLevel));
        }
    }
}
