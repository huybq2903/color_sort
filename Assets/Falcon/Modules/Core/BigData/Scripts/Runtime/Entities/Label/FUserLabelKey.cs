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
    /// Nhãn NGƯỜI CHƠI đã đăng ký trong hợp đồng — tên do SDK cấp nên cả fleet gọi giống nhau và
    /// server so được chéo game. Nhãn tự do vẫn gõ tên tuỳ ý qua <c>Label.User(...)</c>.
    /// <br/>Đây là chữ trên dây, KHÔNG phải tên field C#.
    /// </summary>
    public static class FUserLabelKey
    {
        /// <summary>
        /// Điểm trình độ người chơi. Trước 1.3.0 nó là field phẳng trên mọi level event — sai chủ
        /// thể (§H6: elo tả NGƯỜI CHƠI, không tả lượt) và nhân bản một sự thật của user lên stream
        /// dày nhất hệ. Nay gửi KHI ĐỔI, server forward-fill lên các event sau.
        /// </summary>
        public const string ELO = "elo";
    }
}
