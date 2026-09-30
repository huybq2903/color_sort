using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

// Tool 1 lần: vá lại các group Localization bị mất BundledAssetGroupSchema
// (reference schema từng bị wipe khi Unity re-serialize). Group thiếu schema sẽ
// không được build vào catalog -> device báo InvalidKeyException / No Locales.
public static class FixLocalizationGroupSchemas
{
    // Phải là group Localization CÒN nguyên schema để copy Build/Load path + compression.
    // Không dùng Localization-Assets-* vì chính nhóm đó đang mất schema.
    private const string TemplateGroupName = "Localization-String-Tables-English (en)";

    [MenuItem("Tools/Addressables/Fix Missing Group Schemas")]
    private static void Fix()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[FixSchemas] Không tìm thấy AddressableAssetSettings.");
            return;
        }

        // Lấy 1 group Localization còn nguyên schema làm mẫu để copy cấu hình Local path/compression.
        var template = settings.FindGroup(TemplateGroupName);
        var tmpl = template != null ? template.GetSchema<BundledAssetGroupSchema>() : null;

        int fixedCount = 0;
        foreach (var group in settings.groups)
        {
            if (group == null || !group.Name.StartsWith("Localization")) continue;
            if (group.HasSchema<BundledAssetGroupSchema>()) continue;

            var bundled = group.AddSchema<BundledAssetGroupSchema>();
            if (tmpl != null)
            {
                bundled.BuildPath.SetVariableById(settings, tmpl.BuildPath.Id);
                bundled.LoadPath.SetVariableById(settings, tmpl.LoadPath.Id);
                bundled.Compression = tmpl.Compression;
                bundled.IncludeInBuild = tmpl.IncludeInBuild;
                bundled.BundleMode = tmpl.BundleMode;
                bundled.BundleNaming = tmpl.BundleNaming;
                bundled.InternalBundleIdMode = tmpl.InternalBundleIdMode;
                bundled.InternalIdNamingMode = tmpl.InternalIdNamingMode;
            }
            else
            {
                bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
                bundled.IncludeInBuild = true;
            }

            if (!group.HasSchema<ContentUpdateGroupSchema>())
                group.AddSchema<ContentUpdateGroupSchema>();

            EditorUtility.SetDirty(group);
            fixedCount++;
            Debug.Log($"[FixSchemas] Đã thêm schema cho group: {group.Name}");
        }

        if (fixedCount > 0)
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
        Debug.Log($"[FixSchemas] Hoàn tất. Số group được vá: {fixedCount}. " +
                  "Nhớ Build Addressables lại (target Android) trước khi build APK.");
    }
}
