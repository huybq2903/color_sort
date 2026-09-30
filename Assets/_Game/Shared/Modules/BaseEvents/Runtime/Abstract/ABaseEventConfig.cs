/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-09
 */

using System;
using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Falcon.Shared.BaseEvents
{
    public abstract class ABaseEventConfig 
    {
        [InfoBox("Editor Only"), JsonIgnore]
        public int fakeDurationEvent = -1;
        
        [InfoBox("Config")]
        public ObscuredInt levelShow;
        public ObscuredInt levelUnlock;
    }
    
    /// <summary>
    /// Định nghĩa một phần thưởng trong event.
    /// </summary>
    [Serializable]
    public class Reward
    {
        /// <summary>
        /// Tên phần thưởng.
        /// </summary>
        [ValueDropdown("GetRewardNames")]
        public ObscuredString name = string.Empty;

#if UNITY_EDITOR
        private static EventRewardNameRegistry _cachedRegistry;
        private static bool _cacheInitialized;

        static Reward()
        {
            EditorApplication.projectChanged += () => { _cacheInitialized = false; _cachedRegistry = null; };
        }

        private static EventRewardNameRegistry GetRegistry()
        {
            if (_cacheInitialized) return _cachedRegistry;
            _cacheInitialized = true;
            var guids = AssetDatabase.FindAssets("t:EventRewardNameRegistry");
            if (guids.Length == 0) return null;
            _cachedRegistry = AssetDatabase.LoadAssetAtPath<EventRewardNameRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return _cachedRegistry;
        }

        [OnInspectorGUI]
        private void DrawMissingRegistryWarning()
        {
            if (GetRegistry()) return;
            EditorGUILayout.HelpBox("Chưa có EventRewardNameRegistry asset.\nTạo mới qua Assets → Create → Event Reward Name Registry.", MessageType.Warning);
        }

        private static IEnumerable<ValueDropdownItem<ObscuredString>> GetRewardNames()
        {
            var names = GetRegistry()?.names;
            if (names == null) yield break;
            foreach (var n in names) yield return new ValueDropdownItem<ObscuredString>(n, n);
        }
#endif

        /// <summary>
        /// Số lượng phần thưởng.
        /// </summary>
        public ObscuredInt amount;

        /// <summary>
        /// Dữ liệu phụ đi kèm phần thưởng (tùy biến theo ngữ cảnh).
        /// </summary>
        public ObscuredString data = string.Empty;
        
        public static (string, int, string)[] ToTupleArray(Reward[] rewards)
        {
            return rewards.Select(r => ((string)r.name, (int)r.amount, (string)r.data)).ToArray();
        }
    }
}
