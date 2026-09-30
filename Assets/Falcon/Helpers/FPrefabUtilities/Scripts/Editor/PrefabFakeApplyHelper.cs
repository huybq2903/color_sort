using UE = UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using Falcon.Helpers.FPrefabUtilities.Runtime;
using System.IO;
using UnityEditor.SceneManagement;

namespace Falcon.Helpers.FPrefabUtilities.Editor
{
    /// <summary>
    /// Helper chung: Fake Apply = snapshot overrides → ApplyPrefabInstance → restore overrides.
    /// Dùng trên PREFAB INSTANCE ROOT.
    /// </summary>
    public static class PrefabFakeApplyHelper
    {
        public static void RunFakeApplyOnInstance(GameObject instanceRoot, bool pingAsset = true, bool needBackup = true)
        {
            if (instanceRoot == null) return;

            var instRoot = UE.PrefabUtility.GetNearestPrefabInstanceRoot(instanceRoot);
            if (instRoot == null)
            {
                Debug.LogWarning("[FakeApply] Not a prefab instance. Skipped.");
                return;
            }

            // 0) đảm bảo có backup trước khi Apply
            if (needBackup)
            {
                var backupPath = EnsureBackupPrefabForApply(instRoot);
                if (!string.IsNullOrEmpty(backupPath))
                    Debug.Log($"[FakeApply] Backup ensured at: {backupPath}");
            }

            // 1) Snapshot
            var snapshot = SnapshotOverrides(instRoot);

            // 2) Apply instance -> prefab asset
            UE.PrefabUtility.ApplyPrefabInstance(instRoot, UE.InteractionMode.AutomatedAction);

            // 3) Restore overrides
            RestoreOverrides(snapshot);

            if (pingAsset)
            {
                var prefabAsset = UE.PrefabUtility.GetCorrespondingObjectFromSource(instRoot);
                if (prefabAsset != null) UE.EditorGUIUtility.PingObject(prefabAsset);
            }
        }

        // (tuỳ chọn) chống bấm liên tục khi đang xử lý
        static bool _isApplying;
        public static void ForceFakeApply(GameObject prefabOrInstance)
        {
            if (!prefabOrInstance) return;

            //Nếu là prefab asset (variant) → mở Prefab Mode, chạy trên root của stage
            var variantPath = AssetDatabase.GetAssetPath(prefabOrInstance);
            if (!string.IsNullOrEmpty(variantPath))
            {
                if (_isApplying) return; // tránh re-entrancy khi bấm liên tục
                _isApplying = true;

                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        // Nếu đang ở Prefab Mode khác → quay về Main trước
                        var current = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                        if (current != null && !string.Equals(current.assetPath, variantPath, System.StringComparison.Ordinal))
                            UnityEditor.SceneManagement.StageUtility.GoToMainStage();

                        // 1) Mở variant vào Prefab Mode
                        var stage = UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(variantPath);
                        if (stage == null || stage.prefabContentsRoot == null)
                        {
                            Debug.LogWarning($"[ForceFakeApply] Không mở được Prefab Mode: {variantPath}");
                            return;
                        }

                        // 2) Fake Apply trên root của Prefab Mode (root lúc này là instance của base)
                        RunFakeApplyOnInstance(stage.prefabContentsRoot, pingAsset: false, needBackup: false);

                        // 3) Ghi lại asset
                        PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
                    }
                    finally
                    {
                        // 4) Thoát về Main Stage + refresh
                        //UnityEditor.SceneManagement.StageUtility.GoToMainStage();
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                        _isApplying = false;
                    }
                };

                return;
            }

            Debug.LogWarning("[ForceFakeApply] Input không phải instance cũng không phải prefab asset. Bỏ qua.");
        }
        // ===== Snapshot → Restore =====

        private static Dictionary<Object, UE.PropertyModification[]> SnapshotOverrides(GameObject root)
        {
            var dict = new Dictionary<Object, UE.PropertyModification[]>();

            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                SnapObject(dict, tr.gameObject);

                var components = tr.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    var comp = components[i];
                    if (comp != null) SnapObject(dict, comp);
                }
            }
            return dict;
        }

        private static void SnapObject(Dictionary<Object, UE.PropertyModification[]> dict, Object obj)
        {
            var mods = UE.PrefabUtility.GetPropertyModifications(obj);
            if (mods != null && mods.Length > 0)
                dict[obj] = (UE.PropertyModification[])mods.Clone();
        }

        private static void RestoreOverrides(Dictionary<Object, UE.PropertyModification[]> snapshot)
        {
            foreach (var kv in snapshot)
            {
                if (kv.Key == null) continue; // Có thể bị Destroy khi Apply
                UE.PrefabUtility.SetPropertyModifications(kv.Key, kv.Value);
            }
        }

        public static void FakeApplyAll(string PrefabsFolder)
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsFolder });
            int total = guids.Length, updated = 0, skipped = 0, failed = 0;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    string guid = guids[i];
                    string variantPath = AssetDatabase.GUIDToAssetPath(guid);

                    // Chỉ xử lý prefab variant có gắn AutoFakeApplyComponent ở ROOT
                    var variantAsset = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
                    if (variantAsset == null || variantAsset.GetComponent<FakeApplyMarker>() == null)
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        EditorUtility.DisplayProgressBar(
                            "Fake Apply (Prefab Mode)",
                            $"{i + 1}/{total}  {variantPath}",
                            (i + 1f) / total
                        );

                        // 1) Mở variant vào Prefab Mode
                        var stage = UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(variantPath);
                        if (stage == null || stage.prefabContentsRoot == null)
                        {
                            failed++;
                            continue;
                        }

                        // 2) Fake Apply trên root (trong Prefab Mode, variant root là instance của base)
                        PrefabFakeApplyHelper.RunFakeApplyOnInstance(stage.prefabContentsRoot, pingAsset: false);

                        // 3) Ghi prefab asset
                        PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);

                        // 4) Thoát về Main Stage
                        UnityEditor.SceneManagement.StageUtility.GoToMainStage();

                        updated++;
                        Debug.Log($"[FakeApply PrefabMode] {variantPath} done.");
                    }
                    catch (System.Exception ex)
                    {
                        failed++;
                        Debug.LogException(ex);
                        UnityEditor.SceneManagement.StageUtility.GoToMainStage();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                UnityEditor.SceneManagement.StageUtility.GoToMainStage();
                Debug.Log($"[FakeApply PrefabMode] Finished. Updated: {updated}, Skipped: {skipped}, Failed: {failed}");
            }
        }

        [MenuItem("Falcon/Fake Apply All (Selected Folders)")]
        public static void FakeApplyAll_SelectedFolders()
        {
            var guids = Selection.assetGUIDs;
            var folders = new List<string>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                    folders.Add(path);
            }

            if (folders.Count == 0)
            {
                EditorUtility.DisplayDialog("Fake Apply", "Hãy chọn ít nhất 1 folder trong Project window.", "OK");
                return;
            }

            try
            {
                for (int i = 0; i < folders.Count; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Fake Apply All Prefabs",
                        $"Đang xử lý: {folders[i]} ({i + 1}/{folders.Count})",
                        (i + 1f) / folders.Count
                    );
                    FakeApplyAll(folders[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Fake Apply", $"Đã chạy Fake Apply cho {folders.Count} folder.", "OK");
        }

        #region Backup

        /// <summary>
        /// Tạo 1 bản backup độc lập của prefab asset bị Apply (nếu chưa có).
        /// Đặt tên: "<BaseName>_backup.prefab" cùng thư mục với base.
        /// </summary>
        public static string EnsureBackupPrefabForApply(GameObject instanceRoot, string backupDir = null)
        {
            var targetAssetRoot = UE.PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
            if (targetAssetRoot == null) return null;

            var basePath = AssetDatabase.GetAssetPath(targetAssetRoot);
            if (string.IsNullOrEmpty(basePath)) return null;

            var fileNoExt = Path.GetFileNameWithoutExtension(basePath);
            var ext = Path.GetExtension(basePath);

            string dirToUse;
            if (!string.IsNullOrEmpty(backupDir))
            {
                dirToUse = backupDir.Replace("\\", "/");

                if (!AssetDatabase.IsValidFolder(dirToUse))
                {
                    // Tạo folder nhiều cấp nếu chưa có
                    var parts = dirToUse.Split('/');
                    var current = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var next = $"{current}/{parts[i]}";
                        if (!AssetDatabase.IsValidFolder(next))
                            AssetDatabase.CreateFolder(current, parts[i]);
                        current = next;
                    }
                }
            }
            else
            {
                dirToUse = Path.GetDirectoryName(basePath)?.Replace("\\", "/");
            }

            var desiredBackupPath = $"{dirToUse}/{fileNoExt}_backup{ext}";

            // Đã có sẵn backup → bỏ qua
            if (AssetDatabase.LoadAssetAtPath<GameObject>(desiredBackupPath) != null)
                return desiredBackupPath;

            // Cách chính: deep copy asset → backup độc lập, an toàn
            if (AssetDatabase.CopyAsset(basePath, desiredBackupPath))
            {
                AssetDatabase.ImportAsset(desiredBackupPath);
                return desiredBackupPath;
            }

            return null;
        }

        /// <summary>
        /// Lấy đường dẫn base GẦN NHẤT (direct parent) cho context hiện tại.
        /// Hoạt động cả Scene (instance) lẫn Prefab Mode (stage root).
        /// Trả về true nếu object thuộc một Prefab Variant (có parent), kèm nearestBasePath.
        /// </summary>
        public static bool TryGetNearestParentBaseForContext(GameObject contextGo, out string nearestBasePath)
        {
            nearestBasePath = null;
            if (contextGo == null) return false;

            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();

            if (stage != null && ReferenceEquals(contextGo, stage.prefabContentsRoot))
            {
                // Prefab Mode: root đang hiển thị là nội dung variant; ở đây GetCorrespondingObjectFromSource(root)
                // sẽ trả về CHÍNH direct parent (nearest), không phải original.
                var parent = UE.PrefabUtility.GetCorrespondingObjectFromSource(stage.prefabContentsRoot);
                if (parent == null) return false; // không có parent → không phải variant
                nearestBasePath = UE.AssetDatabase.GetAssetPath(parent);
                return !string.IsNullOrEmpty(nearestBasePath);
            }

            // Scene (instance)
            var outer = UE.PrefabUtility.GetOutermostPrefabInstanceRoot(contextGo);
            if (outer == null || !ReferenceEquals(outer, contextGo)) return false;

            // Lấy asset path của prefab mà instance này trỏ tới (có thể là Variant)
            var asset = UE.PrefabUtility.GetCorrespondingObjectFromSource(outer) as GameObject;
            var variantPath = UE.AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(variantPath)) return false;

            // Để lấy NEAREST parent, cần load contents của CHÍNH asset variant rồi hỏi parent của root đã load.
            try
            {
                var loaded = UE.PrefabUtility.LoadPrefabContents(variantPath);
                try
                {
                    var parent = UE.PrefabUtility.GetCorrespondingObjectFromSource(loaded);
                    if (parent == null) return false; // không phải variant
                    nearestBasePath = UE.AssetDatabase.GetAssetPath(parent);
                    return !string.IsNullOrEmpty(nearestBasePath);
                }
                finally
                {
                    UE.PrefabUtility.UnloadPrefabContents(loaded);
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetBackupPathForBase(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) return null;
            var dir = Path.GetDirectoryName(basePath)?.Replace("\\", "/");
            var name = Path.GetFileNameWithoutExtension(basePath);
            var ext = Path.GetExtension(basePath);
            return string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(ext)
                ? null : $"{dir}/{name}_backup{ext}";
        }

        public static bool BackupExists(string backupPath)
            => !string.IsNullOrEmpty(backupPath) &&
               UE.AssetDatabase.LoadAssetAtPath<GameObject>(backupPath) != null;

        /// <summary>Ghi đè nội dung base bằng backup (giữ nguyên GUID base). Không confirm.</summary>
        public static bool ResetBaseFromBackup(string basePath, string backupPath)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(backupPath))
            {
                Debug.LogWarning("[ResetFromBackup] Path rỗng.");
                return false;
            }

            try
            {
                EnsureSafeStageForReset(basePath, backupPath);

                var loaded = UE.PrefabUtility.LoadPrefabContents(backupPath);
                try
                {
                    var saved = UE.PrefabUtility.SaveAsPrefabAsset(loaded, basePath); // giữ GUID
                    if (saved == null)
                    {
                        Debug.LogError("[ResetFromBackup] SaveAsPrefabAsset thất bại.");
                        return false;
                    }
                }
                finally
                {
                    UE.PrefabUtility.UnloadPrefabContents(loaded);
                }

                UE.AssetDatabase.SaveAssets();
                UE.AssetDatabase.Refresh();
                Debug.Log($"[ResetFromBackup] {backupPath} → {basePath} (preserve GUID)");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }

        /// <summary>Chỉ quay về Main Stage nếu đang mở đúng base hoặc backup để tránh xung đột ghi.</summary>
        private static void EnsureSafeStageForReset(string basePath, string backupPath)
        {
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return;

            if (string.Equals(stage.assetPath, basePath, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stage.assetPath, backupPath, System.StringComparison.OrdinalIgnoreCase))
            {
                UnityEditor.SceneManagement.StageUtility.GoToMainStage();
            }
        }

        public static bool IsUnderModuleWithPackageJson(string assetPath, bool checkAncestors = true)
        {
            // assetPath: đường dẫn của prefab base (ví dụ "Packages/com.foo.bar/Runtime/My.prefab" hoặc "Assets/MyModule/Runtime/My.prefab")
            if (string.IsNullOrEmpty(assetPath)) return false;

            string dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(dir)) return false;

            while (true)
            {
                string packageJsonPath = $"{dir}/package.json";
                // Nếu có file package.json ngay thư mục hiện tại → đây là module
                if (UE.AssetDatabase.LoadAssetAtPath<TextAsset>(packageJsonPath) != null)
                    return true;

                if (!checkAncestors) break; // chỉ kiểm tra đúng thư mục chứa base

                // Lên thư mục cha
                string parent = System.IO.Path.GetDirectoryName(dir)?.Replace("\\", "/");
                if (string.IsNullOrEmpty(parent) || parent == dir) break; // đến root rồi thì dừng
                dir = parent;
            }
            return false;
        }
        #endregion
    }
}