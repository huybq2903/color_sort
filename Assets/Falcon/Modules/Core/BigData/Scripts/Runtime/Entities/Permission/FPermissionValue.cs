/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Giá trị trạng thái quyền/consent (§D8) — vocab ĐÓNG, server đưa thẳng vào <c>sub_event</c>
    /// nên phải đúng từng chữ.
    /// <br/>Loại quyền không có ở đây: nó nằm ở TÊN EVENT, xem <see cref="PermissionType"/>.
    /// </summary>
    public static class FPermissionValue
    {
        // ---- ATT (iOS) ----
        public const string Authorized = "authorized";
        public const string Restricted = "restricted";
        public const string NotDetermined = "not_determined";

        // ---- Đồng ý quảng cáo / quyền push ----
        public const string Granted = "granted";

        /// <summary>Dùng chung cho cả ba loại quyền.</summary>
        public const string Denied = "denied";
    }
}
