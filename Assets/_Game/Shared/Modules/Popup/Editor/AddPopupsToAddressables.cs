/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-29
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Falcon.Shared.EasyPopup
{
    public class AddPopupsToAddressable
    {
        [MenuItem("Tools/Addressables/Add all UIPopupBase prefabs to 'Popup' group")]
        public static void AddAllPopups()
        {
            // Lấy settings Addressables (tạo nếu chưa có)
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found or failed to create.");
                return;
            }

            // Tìm (hoặc tạo) group "Popup"
            var group = settings.FindGroup("Popup");
            if (!group)
            {
                // Tạo group với schema cơ bản để bundle
                var schemas = new List<AddressableAssetGroupSchema>();
                group = settings.CreateGroup(
                    "Popup",
                    false, // readOnly
                    false, // postEvent
                    false, // readOnlyWhenUnloaded
                    schemas,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema)
                );
                Debug.Log("Created Addressables group: Popup");
            }

            // Bảo đảm có nhãn "Popup"
            settings.AddLabel("Popup", false);

            int found = 0, added = 0, already = 0, errors = 0;

            // Quét tất cả prefab trong Assets
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                // Kiểm tra xem prefab có component kế thừa UIPopup_Base hay không
                // (GetComponentInChildren / true để bắt cả disabled & nested)
                var popup = go.GetComponentInChildren<UIPopupBase>(true);
                if (!popup) continue;

                found++;

                // Tạo/move entry vào group "Popup"
                try
                {
                    var entry = settings.FindAssetEntry(guid);
                    if (entry != null && entry.parentGroup == group)
                    {
                        // Đã ở đúng group
                        already++;
                    }
                    else
                    {
                        entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                        added++;

                        // Đặt address = tên file (không có đuôi)
                        // string address = Path.GetFileNameWithoutExtension(path);
                        entry.SetAddress(popup.GetType().Name);

                        // Gắn label "Popup" để lọc nhanh
                        entry.SetLabel("Popup", true);
                    }
                }
                catch (System.SystemException ex)
                {
                    errors++;
                    Debug.LogError($"Failed to add '{path}' to Addressables: {ex.Message}");
                }
            }

            // Lưu & refresh
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[AddPopupsToAddressables] Found: {found}, Added/Moved: {added}, Already in group: {already}, Errors: {errors}");
            EditorUtility.DisplayDialog("Add Popups to Addressables",
                $"Đã quét prefab kế thừa UIPopup_Base.\n" +
                $"Tìm thấy: {found}\n" +
                $"Đã thêm/chuyển: {added}\n" +
                $"Đã ở group: {already}\n" +
                $"Lỗi: {errors}",
                "OK");
        }
    }
}