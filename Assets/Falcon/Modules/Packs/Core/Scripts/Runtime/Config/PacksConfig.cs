/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using Newtonsoft.Json;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Cấu hình tổng thể của một nhóm pack.
    /// Mỗi nhóm được xác định bởi idGroup và chứa danh sách các phần tử (packs).
    /// </summary>
    [JsonConverter(typeof(PacksConfigConverter))]
    public class PacksConfig
    {
        /// <summary>
        /// Định danh duy nhất cho nhóm pack.
        /// Dùng để phân loại wrapper, config và userData.
        /// </summary>
        public string idGroup;

        /// <summary>
        /// Danh sách các pack con thuộc nhóm này.
        /// </summary>
        public ABaseElementPackConfig[] elements;
    }
}