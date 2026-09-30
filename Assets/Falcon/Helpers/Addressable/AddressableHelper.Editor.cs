/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-05
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Falcon.Helpers.Addressable
{
    // Phần authoring chỉ-Editor của AddressableHelper (tách khỏi API load/release runtime).
    public static partial class AddressableHelper
    {
        private static AddressableAssetSettings CreateDefaultSettings()
        {
            AddressableAssetSettingsDefaultObject.Settings =
                AddressableAssetSettings.Create(
                    AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                    AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                    true, true);
            UnityEditor.AssetDatabase.SaveAssets();
            return AddressableAssetSettingsDefaultObject.Settings;
        }

        private static AddressableAssetGroup GetOrCreateGroup(
            AddressableAssetSettings settings, string groupName)
        {
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                var schemas = settings.DefaultGroup != null ? settings.DefaultGroup.Schemas : null;
                if (schemas == null || schemas.Count == 0)
                    Debug.LogWarning($"{nameof(AddressableHelper)} > GetOrCreateGroup: DefaultGroup thiếu schema → group '{groupName}' tạo KHÔNG schema (không build/load được). Cấu hình schema thủ công.");
                group = settings.CreateGroup(groupName, false, false, false, schemas);
            }
            return group;
        }

        // Resolve settings (tạo default nếu chưa có). false + log nếu không tạo được.
        private static bool TryGetOrCreateSettings(string caller,
            out AddressableAssetSettings settings)
        {
            settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) settings = CreateDefaultSettings(); // == null: dùng Unity override (không ?? trên UnityEngine.Object)
            if (settings != null) return true;
            Debug.LogError($"{nameof(AddressableHelper)} > {caller}: không tạo được AddressableAssetSettings → bỏ qua.");
            return false;
        }

        // Tạo/di chuyển entry cho 1 obj + áp simplifyName chống trùng address qua 'taken' (null ⇒ bỏ simplify). Dùng chung single & batch.
        private static void ApplyAddressable(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            UnityEngine.Object obj, bool simplifyName, Dictionary<string, string> taken, string caller)
        {
            if (!obj)
            {
                Debug.LogError($"{nameof(AddressableHelper)} > {caller}: obj null hoặc đã huỷ → bỏ qua.");
                return;
            }
            var objName = obj.name;
            var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"{nameof(AddressableHelper)} > {caller}: '{objName}' không phải asset đã lưu → bỏ qua.");
                return;
            }
            var entry = settings.CreateOrMoveEntry(UnityEditor.AssetDatabase.AssetPathToGUID(path), group);
            if (entry == null)
            {
                Debug.LogError($"{nameof(AddressableHelper)} > {caller}: CreateOrMoveEntry trả null cho '{objName}' (group '{group.Name}') → bỏ qua.");
                return;
            }
            if (!simplifyName) return;
            if (taken.TryGetValue(objName, out var owner) && owner != entry.guid)
                Debug.LogError($"{nameof(AddressableHelper)} > {caller}: address '{objName}' đã bị entry khác chiếm → bỏ simplifyName cho '{objName}', giữ address mặc định tránh đụng độ.");
            else
            {
                entry.address = objName;
                taken[objName] = entry.guid; // chặn trùng giữa các asset trong cùng batch
            }
        }

        // Snapshot address→guid của toàn bộ entry (1 lần, O(E)) cho batch dò trùng O(1). Bỏ entry address rỗng.
        private static Dictionary<string, string> BuildAddressMap(
            AddressableAssetSettings settings)
        {
            var map = new Dictionary<string, string>();
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                // @by-design: entries=ICollection<T> ⇒ box enumerator; editor-only, không đáng né.
                foreach (var e in group.entries)
                    if (!string.IsNullOrEmpty(e.address))
                        map[e.address] = e.guid; // data lỗi có thể trùng address sẵn → giữ guid cuối, đủ để dò "đã bị chiếm"
            }
            return map;
        }

        public static void MakeAssetAddressable(UnityEngine.Object obj, string groupName, bool simplifyName, bool save = true)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                Debug.LogError($"{nameof(AddressableHelper)} > MakeAssetAddressable: groupName null/rỗng cho '{(obj ? obj.name : "null")}' → bỏ qua.");
                return;
            }
            if (!TryGetOrCreateSettings(nameof(MakeAssetAddressable), out var settings)) return;
            var group = GetOrCreateGroup(settings, groupName);
            // @by-design: single dựng full map cho gọn
            var taken = simplifyName ? BuildAddressMap(settings) : null;
            ApplyAddressable(settings, group, obj, simplifyName, taken, nameof(MakeAssetAddressable));
            UnityEditor.EditorUtility.SetDirty(settings);
            if (save)
                UnityEditor.AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Batch của <see cref="MakeAssetAddressable"/>: dựng map address→guid 1 LẦN (O(E)) rồi tra O(1)/asset,
        /// SaveAssets() 1 lần cuối — tránh O(N×E) scan + N×SaveAssets khi authoring nhiều asset cùng lúc.
        /// </summary>
        public static void MakeAssetsAddressable(IReadOnlyList<UnityEngine.Object> objs, string groupName, bool simplifyName)
        {
            if (objs == null || objs.Count == 0) return;
            if (string.IsNullOrWhiteSpace(groupName))
            {
                Debug.LogError($"{nameof(AddressableHelper)} > MakeAssetsAddressable: groupName null/rỗng → bỏ qua {objs.Count} asset.");
                return;
            }
            if (!TryGetOrCreateSettings(nameof(MakeAssetsAddressable), out var settings)) return;
            var group = GetOrCreateGroup(settings, groupName);
            var taken = simplifyName ? BuildAddressMap(settings) : null; // chỉ cần map khi simplifyName

            for (int i = 0; i < objs.Count; i++) // for + indexer: tránh box enumerator của IReadOnlyList<T>
                ApplyAddressable(settings, group, objs[i], simplifyName, taken, nameof(MakeAssetsAddressable));

            UnityEditor.EditorUtility.SetDirty(settings);
            UnityEditor.AssetDatabase.SaveAssets(); // 1 lần cho cả batch
        }

        public static bool IsInAddressables(UnityEngine.Object obj)
        {
            if (!obj) return false; // null/fake-null: không phải asset hợp lệ → không in-addressables
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) return false;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false; // chưa cấu hình Addressables ⇒ asset không thể in-addressables (câu trả lời đúng của query)

            var entry = settings.FindAssetEntry(UnityEditor.AssetDatabase.AssetPathToGUID(assetPath));
            return entry != null;
        }
    }
}
#endif
