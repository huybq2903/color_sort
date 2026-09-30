/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-10
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Trạng thái ATT của iOS (§D8) — 4 giá trị đóng, khớp 1-1 với
    /// <c>ATTrackingManagerAuthorizationStatus</c> của Apple.
    /// <br/>Có enum để game khỏi gõ chuỗi tay: vocab này server đưa thẳng vào <c>sub_event</c> nên
    /// sai một chữ là mất cả cohort (luật §H2 — SDK cấp constant, cấm string tay).
    /// </summary>
    public enum AttStatus
    {
        /// <summary>Chưa hỏi, hoặc user chưa trả lời.</summary>
        NotDetermined,

        /// <summary>Bị chặn ở tầng thiết bị (parental control, MDM) — hỏi cũng không hiện.</summary>
        Restricted,

        Denied,

        Authorized
    }

    public static class AttStatusExtensions
    {
        /// <summary>Giá trị lên wire — xem <see cref="FPermissionValue"/>.</summary>
        public static string ToPermissionValue(this AttStatus status)
        {
            return status switch
            {
                AttStatus.Authorized => FPermissionValue.Authorized,
                AttStatus.Denied => FPermissionValue.Denied,
                AttStatus.Restricted => FPermissionValue.Restricted,
                _ => FPermissionValue.NotDetermined
            };
        }
    }
}
