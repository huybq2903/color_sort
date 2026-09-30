/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-10
 */

using Newtonsoft.Json;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Lớp đại diện cho một nhóm pack, bao gồm cả thông tin cấu hình và dữ liệu người dùng tương ứng.
    /// Dữ liệu này có thể đến từ local hoặc được đồng bộ từ server khi đăng nhập.
    /// </summary>
    [JsonConverter(typeof(PacksInfoConverter))]
    public class PacksInfo
    {
        /// <summary>
        /// Cấu hình của nhóm pack.
        /// Chứa thông tin về các gói phần tử và định danh nhóm (idGroup).
        /// </summary>
        public PacksConfig config;
        
        /// <summary>
        /// Dữ liệu cá nhân hóa theo người dùng, ví dụ: tiến độ, phần thưởng đã nhận...
        /// </summary>
        public IPackUserData userData;
    }
}