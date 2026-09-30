/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-10
 */

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Đại diện cho trạng thái dữ liệu người dùng trống hoặc không tồn tại cho packs.
    /// 
    /// Sử dụng lớp này cho các nhóm pack không yêu cầu lưu trữ dữ liệu cụ thể của người dùng.
    /// Ví dụ: các pack tĩnh hoặc pack không thay đổi theo người dùng.
    /// </summary>
    public class NullPackUserData : ABasePackUserData
    {
        
    }
}