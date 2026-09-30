/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Lớp cấu hình cơ bản cho một phần tử trong nhóm pack.
    /// Được kế thừa bởi các cấu hình cụ thể của từng loại pack.
    /// Chứa thông tin về phần thưởng, định danh và ID sản phẩm.
    /// </summary>
    public abstract class ABaseElementPackConfig
    {
        /// <summary>
        /// ID duy nhất định danh pack.
        /// </summary>
        public string idPack;

        /// <summary>
        /// ID sản phẩm, dùng cho giao dịch InApp Purchase.
        /// </summary>
        public string productId;

        /// <summary>
        /// Danh sách phần thưởng người chơi sẽ nhận khi mua pack.
        /// </summary>
        public Reward[] rewards;

        /// <summary>
        /// Định nghĩa một phần thưởng trong pack.
        /// </summary>
        [Serializable]
        public class Reward
        {
            /// <summary>
            /// Tên phần thưởng.
            /// </summary>
            [ValueDropdown("GetRewardNames")]
            public string name;

#if UNITY_EDITOR
            private static RewardNameRegistry _cachedRegistry;
            private static bool _cacheInitialized;

            static Reward()
            {
                EditorApplication.projectChanged += () => { _cacheInitialized = false; _cachedRegistry = null; };
            }

            private static RewardNameRegistry GetRegistry()
            {
                if (_cacheInitialized) return _cachedRegistry;
                _cacheInitialized = true;
                var guids = AssetDatabase.FindAssets("t:RewardNameRegistry");
                if (guids.Length == 0) return null;
                _cachedRegistry = AssetDatabase.LoadAssetAtPath<RewardNameRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
                return _cachedRegistry;
            }

            [OnInspectorGUI]
            private void DrawMissingRegistryWarning()
            {
                if (GetRegistry()) return;
                EditorGUILayout.HelpBox("Chưa có RewardNameRegistry asset.\nTạo mới qua Assets → Create → Pack → Reward Name Registry.", MessageType.Warning);
            }
            
            private static IEnumerable<string> GetRewardNames()
            {
                var registry = GetRegistry();
                return registry?.names ?? Array.Empty<string>();
            }
#endif

            /// <summary>
            /// Số lượng phần thưởng.
            /// </summary>
            public int amount;

            /// <summary>
            /// Dữ liệu phụ đi kèm phần thưởng (tùy biến theo ngữ cảnh).
            /// </summary>
            public string data;
        }
    }
}