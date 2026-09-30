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
    /// Loại quyền/consent (§D8). Mỗi loại là MỘT EVENT RIÊNG (<c>f_sdk_permission_att</c>,
    /// <c>f_sdk_permission_ads_consent</c>, <c>f_sdk_permission_push</c>) chứ không phải một event
    /// chung mang key trong <c>sub_event</c>.
    /// <br/>Luật phân xử của hợp đồng: <b>vocab ĐÓNG toàn-fleet mà analyst lọc trực tiếp thì nằm
    /// trong TÊN EVENT; vocab MỞ per-game thì nằm trong param.</b> Danh sách quyền là đóng (5-7 cái
    /// là kịch) nên tách; còn <c>funnelName</c> mỗi game một bộ nên để trong param.
    /// <br/>Phí migrate bất đối xứng chốt án này: gộp rồi muốn tách là cả một chiến dịch convert
    /// (án <c>f_sdk_level</c> phải tách thành 8 event), còn tách rồi muốn gộp thì miễn phí —
    /// <c>event LIKE 'f_sdk_permission_%'</c> quét cả họ.
    /// </summary>
    public enum PermissionType
    {
        /// <summary>ATT trên iOS — quyết định trực tiếp eCPM và match-rate attribution.</summary>
        Att,

        /// <summary>Đồng ý quảng cáo (GDPR/UMP).</summary>
        AdsConsent,

        /// <summary>Quyền nhận push — mẫu số của mọi phân tích push.</summary>
        Push
    }

    public static class PermissionTypeExtensions
    {
        /// <summary>Tên event trên wire — mỗi loại quyền một event (§D8).</summary>
        public static string ToEventId(this PermissionType type)
        {
            return type switch
            {
                PermissionType.Att => "f_sdk_permission_att",
                PermissionType.AdsConsent => "f_sdk_permission_ads_consent",
                PermissionType.Push => "f_sdk_permission_push",
                _ => "f_sdk_permission_unknown"
            };
        }
    }
}
