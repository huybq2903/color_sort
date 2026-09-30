/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-11
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Nhãn màn ĐÃ ĐĂNG KÝ trong hợp đồng — tên do SDK cấp nên mọi game gọi giống nhau và server
    /// so được chéo game (khác nhãn tự do, vốn chỉ có nghĩa trong nội bộ một game).
    /// <br/>Chúng nằm trong cùng bundle <c>levelLabels</c> với nhãn tự do; khác biệt duy nhất là
    /// tên đã chốt và SDK giữ chỗ byte cho chúng (xem
    /// <see cref="EntityLabelState.RESERVED_REGISTERED_BYTES"/>).
    /// <br/>Đây là chữ trên dây, KHÔNG phải tên field C# — nên viết snake_case đúng như hợp đồng.
    /// </summary>
    public static class FLevelLabelKey
    {
        /// <summary>
        /// Giới hạn lượt đi của màn (config màn). Mẫu số cho <c>movesUsed</c>: "pass còn thừa mấy
        /// lượt" chính là thước đo độ dễ.
        /// </summary>
        public const string MOVES_LIMIT = "moves_limit";

        /// <summary>Giới hạn thời gian của màn, tính bằng giây — vai trò như <see cref="MOVES_LIMIT"/>.</summary>
        public const string TIME_LIMIT_SEC = "time_limit_sec";
    }
}
