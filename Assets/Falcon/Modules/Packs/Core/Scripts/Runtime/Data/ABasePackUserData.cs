/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-10
 */

using Sirenix.OdinInspector;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Lớp cơ sở cho tất cả các loại dữ liệu người dùng liên quan đến nhóm pack.
    /// Chứa thông tin định danh và số thứ tự dùng để đồng bộ hóa với server.
    /// </summary>
    public abstract class ABasePackUserData : IPackUserData
    {
        /// <summary>
        /// Khóa nhóm pack (idGroup).
        /// </summary>
        [ReadOnly] public string idGroup;

        /// <summary>
        /// Số thứ tự dùng để kiểm tra dữ liệu mới nhất khi đồng bộ (sequence càng cao càng mới).
        /// </summary>
        [ReadOnly] public int sequence;
    }
    
    public interface IPackUserData {}
}