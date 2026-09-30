/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
 */

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Asset ScriptableObject đại diện cho một nhóm pack trong Unity.
    /// Dễ dàng chỉnh sửa bằng Inspector, giúp designer cấu hình nội dung pack theo nhóm.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_FCM_Packs_Config", menuName = "Pack/PacksConfig")]
    public class SOPacksConfig : SerializedScriptableObject
    {
        /// <summary>
        /// Cấu hình chính của nhóm pack.
        /// Phải là instance của lớp PacksConfig (hoặc kế thừa), chứa idGroup và danh sách phần tử.
        /// </summary>
        public PacksConfig config;
        
        /// <summary>
        /// Cấu hình phụ của nhóm pack đó, để custom thêm nếu cần
        /// </summary>
        public Dictionary<string, string> @params;
    }
}
