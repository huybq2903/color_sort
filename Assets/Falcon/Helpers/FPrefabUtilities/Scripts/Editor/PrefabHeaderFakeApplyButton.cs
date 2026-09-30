#if UNITY_EDITOR
using UE = UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Falcon.Helpers.FPrefabUtilities.Runtime;

namespace Falcon.Helpers.FPrefabUtilities.Editor
{
    /// <summary>
    /// Hiển thị nút "Fake Apply" và "Reset prefab base from backup" ở header Inspector cho OUTERMOST Prefab Instance Root.
    /// </summary>
    [UE.InitializeOnLoad]
    public class PrefabHeaderFakeApplyButton
    {
        static PrefabHeaderFakeApplyButton()
        {
            UE.Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        }

        private static void OnFinishedDefaultHeaderGUI(UE.Editor editor)
        {
            if (!(editor.target is GameObject go)) return;

            // Chỉ hiện trên OUTERMOST prefab instance root
            var root = UE.PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            if (root == null || root != go) return;

            // Không có marker -> ẨN HẲN (không vẽ gì)
            if (!root.TryGetComponent<FakeApplyMarker>(out _))
                return;

            // Nút Fake Apply chỉ enable khi có overrides
            bool hasOverrides = UE.PrefabUtility.HasPrefabInstanceAnyOverrides(root, includeDefaultOverrides: false);

            // Lấy nearest parent base (direct parent) theo ngữ cảnh hiện tại
            bool isVariant = PrefabFakeApplyHelper.TryGetNearestParentBaseForContext(go, out var basePath);
            
            // Nếu bạn chỉ vẽ khi base thuộc module:
            bool baseInModule = isVariant && PrefabFakeApplyHelper.IsUnderModuleWithPackageJson(basePath, checkAncestors: true);
            if (!baseInModule) return;

            string backupPath = isVariant ? PrefabFakeApplyHelper.GetBackupPathForBase(basePath) : null;
            bool hasBackup = isVariant && PrefabFakeApplyHelper.BackupExists(backupPath);

            using (new UE.EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                // --- Nút Fake Apply (giữ nguyên hành vi)
                using (new UE.EditorGUI.DisabledScope(!hasOverrides))
                {
                    var tip = "Apply vào prefab gốc nhưng giữ nguyên các overrides";
                    if (GUILayout.Button(new GUIContent("Fake Apply All to Prefab base (Keep overrides)", tip),
                                         UE.EditorStyles.miniButton))
                    {
                        PrefabFakeApplyHelper.RunFakeApplyOnInstance(root, pingAsset: true);
                        Event.current.Use();
                    }
                }

                GUILayout.Space(4);

                // Reset base from backup (chỉ hiện nếu đây là instance của VARIANT)
                if (isVariant)
                {
                    using (new UE.EditorGUI.DisabledScope(!hasBackup))
                    {
                        var tipReset = hasBackup
                            ? $"Ghi đè BASE bằng BACKUP:\n{backupPath}"
                            : "Không tìm thấy file *_backup.prefab cạnh base.";

                        if (GUILayout.Button(new GUIContent("Reset prefab base from backup", tipReset),
                                             UE.EditorStyles.miniButton))
                        {
                            if (PrefabFakeApplyHelper.ResetBaseFromBackup(basePath, backupPath))
                            {
                                var baseObj = UE.AssetDatabase.LoadAssetAtPath<Object>(basePath);
                                if (baseObj) UE.EditorGUIUtility.PingObject(baseObj);
                            }
                            Event.current.Use();
                        }
                    }
                }
            }

            GUILayout.Space(2);
        }
    }
}
#endif
