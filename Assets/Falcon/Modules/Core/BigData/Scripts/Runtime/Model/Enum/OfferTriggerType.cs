/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    ///     Định nghĩa ngữ cảnh hoặc cơ chế kích hoạt hiển thị Offer tới người dùng.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OfferTriggerType
    {
        /// <summary>
        ///     Chủ động từ phía người dùng (User click vào icon, banner trong shop để xem chi tiết).
        /// </summary>
        UserInitiated,

        /// <summary>
        ///     Hệ thống chủ động kích hoạt tự động dựa trên các sự kiện/mốc thời gian cấu hình sẵn
        ///     (Ví dụ: Đăng nhập đầu ngày, hoàn thành tutorial, lên cấp, kết thúc trận đấu).
        /// </summary>
        SystemTriggered,

        /// <summary>
        ///     Hiển thị theo ngữ cảnh dựa trên hành vi thiếu hụt của người chơi
        ///     (Ví dụ: Người chơi muốn nâng cấp tướng nhưng thiếu Vàng -> Gợi ý gói mua Vàng ngay lập tức).
        /// </summary>
        Contextual,

        /// <summary>
        ///     Kích hoạt từ luồng bên ngoài ứng dụng (Ví dụ: Click từ Push Notification, Deep Link từ quảng cáo/mạng xã hội).
        /// </summary>
        ExternalLink
    }
}