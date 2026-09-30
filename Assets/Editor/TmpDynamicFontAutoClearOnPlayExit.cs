using TMPro;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class TmpDynamicFontAutoClearOnPlayExit
{
    private const string MenuPath = "Tools/TMP/Clear Dynamic Font Asset Data";

    static TmpDynamicFontAutoClearOnPlayExit()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem(MenuPath)]
    private static void ClearDynamicFontAssetDataMenu()
    {
        ClearDynamicFontAssetData(logIfNothingCleared: true);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        ClearDynamicFontAssetData(logIfNothingCleared: false);
    }

    private static void ClearDynamicFontAssetData(bool logIfNothingCleared)
    {
        string[] fontAssetGuids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        int clearedCount = 0;

        foreach (string guid in fontAssetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset == null)
            {
                continue;
            }

            if (!IsDynamicFontAsset(fontAsset))
            {
                continue;
            }

            // true = thu atlas về 0, không giữ texture rỗng full-size trong file
            fontAsset.ClearFontAssetData(true);

            // TMP không tự SetDirty (mọi lời gọi trong TMP_FontAsset và TMP_EditorResourceManager
            // đều bị comment) nên SaveAssets sẽ bỏ qua. Atlas và material là sub-asset, dirty riêng.
            EditorUtility.SetDirty(fontAsset);

            if (fontAsset.atlasTextures != null)
            {
                foreach (Texture2D atlas in fontAsset.atlasTextures)
                {
                    if (atlas != null)
                    {
                        EditorUtility.SetDirty(atlas);
                    }
                }
            }

            if (fontAsset.material != null)
            {
                EditorUtility.SetDirty(fontAsset.material);
            }

            clearedCount++;
        }

        if (clearedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[TMP] Cleared dynamic data for {clearedCount} TMP font asset(s).");
        }
        else if (logIfNothingCleared)
        {
            Debug.Log("[TMP] No dynamic TMP font assets found to clear.");
        }
    }

    private static bool IsDynamicFontAsset(TMP_FontAsset fontAsset)
    {
        return fontAsset.atlasPopulationMode == AtlasPopulationMode.Dynamic
               || fontAsset.atlasPopulationMode == AtlasPopulationMode.DynamicOS;
    }
}
