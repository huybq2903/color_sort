using System.IO;
using UnityEditor;
using UnityEngine;

namespace Falcon.Helpers.FPrefabUtilities.Editor
{
    public static class CreatePrefabVariantsFromFolders
    {
        [MenuItem("Falcon/Create/Prefabs variant from selecteds Folder")]
        private static void CreateVariantsFromSelectedFolders()
        {
            var selections = Selection.GetFiltered<Object>(SelectionMode.Assets);
            if (selections == null || selections.Length == 0)
            {
                EditorUtility.DisplayDialog("Falcon", "Hãy chọn ít nhất 1 thư mục trong Project.", "OK");
                return;
            }

            var folderPaths = System.Array.FindAll(
                System.Array.ConvertAll(selections, AssetDatabase.GetAssetPath),
                AssetDatabase.IsValidFolder
            );

            if (folderPaths.Length == 0)
            {
                EditorUtility.DisplayDialog("Falcon", "Không có thư mục hợp lệ nào được chọn.", "OK");
                return;
            }

            try
            {
                int totalCreated = 0;
                int totalFound = 0;

                for (int i = 0; i < folderPaths.Length; i++)
                {
                    string srcFolder = NormalizePath(folderPaths[i]);
                    string parent = Path.GetDirectoryName(srcFolder)?.Replace("\\", "/");
                    string leaf = Path.GetFileName(srcFolder);
                    string variantLeaf = leaf + "_variant";
                    string dstRoot = string.IsNullOrEmpty(parent) ? variantLeaf : $"{parent}/{variantLeaf}";

                    EnsureFolderExists(parent, variantLeaf, dstRoot);

                    // Tìm tất cả prefab (đệ quy)
                    string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { srcFolder });
                    totalFound += guids.Length;

                    for (int j = 0; j < guids.Length; j++)
                    {
                        string prefabPath = NormalizePath(AssetDatabase.GUIDToAssetPath(guids[j]));
                        EditorUtility.DisplayProgressBar(
                            "Creating Prefab Variants",
                            $"({i + 1}/{folderPaths.Length}) {leaf} → {variantLeaf}\n{Path.GetFileName(prefabPath)}",
                            (i + (j / (float)Mathf.Max(1, guids.Length))) / Mathf.Max(1, folderPaths.Length)
                        );

                        var original = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (original == null)
                            continue;

                        // Tính thư mục đích phản chiếu cấu trúc con
                        string relDir = GetRelativeDirectory(prefabPath, srcFolder); // ví dụ: "Enemies/Flying"
                        string targetDir = string.IsNullOrEmpty(relDir) ? dstRoot : $"{dstRoot}/{relDir}";
                        EnsureFolderPathExists(targetDir);

                        var instance = PrefabUtility.InstantiatePrefab(original) as GameObject;
                        if (instance == null)
                            continue;

                        try
                        {
                            string fileName = Path.GetFileName(prefabPath); // giữ nguyên tên file
                            string targetPath = $"{targetDir}/{fileName}";
                            targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

                            bool success = PrefabUtility.SaveAsPrefabAsset(instance, targetPath, out bool _);
                            if (success) totalCreated++;
                        }
                        finally
                        {
                            if (instance != null)
                                Object.DestroyImmediate(instance);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog(
                    "Falcon",
                    $"Đã quét {totalFound} prefab và tạo {totalCreated} prefab variant.\nCấu trúc thư mục con đã được giữ nguyên trong *_variant*.",
                    "OK"
                );
            }
            catch (System.SystemException e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Falcon - Lỗi", $"Có lỗi xảy ra:\n{e.Message}", "OK");
            }
        }

        // --- Helpers ---

        private static string NormalizePath(string path) => path?.Replace("\\", "/");

        private static string GetRelativeDirectory(string assetPath, string rootFolder)
        {
            string dir = Path.GetDirectoryName(assetPath)?.Replace("\\", "/") ?? string.Empty;
            if (!dir.StartsWith(rootFolder)) return string.Empty;
            string rel = dir.Substring(rootFolder.Length).TrimStart('/'); // có thể rỗng
            return rel;
        }

        private static void EnsureFolderExists(string parent, string newFolderName, string fullPath)
        {
            if (AssetDatabase.IsValidFolder(fullPath))
                return;

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                CreateFoldersRecursively(parent);
            }

            AssetDatabase.CreateFolder(parent, newFolderName);
        }

        private static void EnsureFolderPathExists(string fullPath)
        {
            fullPath = NormalizePath(fullPath);
            if (AssetDatabase.IsValidFolder(fullPath)) return;

            string[] parts = fullPath.Split('/');
            if (parts.Length == 0) return;

            string current = parts[0]; // thường là "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static void CreateFoldersRecursively(string path)
        {
            path = NormalizePath(path);
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0]; // ví dụ "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}