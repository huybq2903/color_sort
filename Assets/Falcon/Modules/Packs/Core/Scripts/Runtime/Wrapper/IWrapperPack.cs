/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-10
 */

using Falcon.Helpers.FReflection;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Interface cho các wrapper pack, cung cấp các thao tác cơ bản
    /// để khởi tạo và truy cập cấu hình cũng như asset config cục bộ.
    /// 
    /// Tất cả các class wrapper pack phải triển khai các thuộc tính và phương thức này.
    /// </summary>
    public interface IWrapperPack : IFReflection
    {
        /// <summary>
        /// Cấu hình nhóm pack hiện tại.
        /// </summary>
        public PacksConfig Config { get; }
        
        /// <summary>
        /// Khóa định danh của nhóm pack (tương ứng với idGroup).
        /// </summary>
        string Key { get; }
        
        /// <summary>
        /// Hàm khởi tạo wrapper pack, được gọi khi tạo từ PacksManager.
        /// </summary>
        void OnInitialize();
        
        /// <summary>
        /// Trả về asset cấu hình cục bộ (SOPacksConfig), nếu có.
        /// </summary>
        SOPacksConfig LocalConfig { get; }
    }
}