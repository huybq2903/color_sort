/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-20
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Vocab <c>InAppParam.validationStatus</c> — phương án B chốt với loader 20/08: log mua bắn
    /// ở MỌI nhánh verdict kèm con dấu này, thay vì bị chặn sau validator (tách analytics khỏi
    /// uptime CSSC, reject hiện hình trong kho, semantics đồng nhất mọi game).
    /// <br/>Cook phía server: revenue_claim = đếm tất; validated revenue = filter status.
    /// <b>Thêm giá trị = SỬA HỢP ĐỒNG</b> (§H2).
    /// </summary>
    public static class FIapValidationStatus
    {
        /// <summary>Validator (CSSC) đã duyệt receipt.</summary>
        public const string Validated = "validated";

        /// <summary>
        /// Validator TỪ CHỐI receipt (fake/replay) — mảng fraud trước đây vô hình vì log bị chặn.
        /// Vẫn là LOG MUA (store-side có giao dịch thật) — không phải billing-fail.
        /// </summary>
        public const string Rejected = "rejected";

        /// <summary>Không liên lạc được validator sau retry — CHƯA soi, không phải giả. Doanh thu claim vẫn ghi nhận, không tụt theo uptime.</summary>
        public const string Unvalidated = "unvalidated";

        /// <summary>Game không đăng ký validator nào — claim thô đúng nghĩa.</summary>
        public const string None = "none";
    }
}
