using Falcon.Modules.LocalizationService.Runtime;
using I2.Loc;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.LocalizationService.Editor
{
    public class AutoI2BinderTools
    {
        [MenuItem("Falcon/Modules/LocalizationService/Localize TMPs in Selecteds(Prefabs and Folders) (skip DoNotLocalize in object's name)")]
        private static void BindSelected()
        {
            // Gom đường dẫn asset cần xử lý để tránh xử lý trùng
            var prefabAssetPaths = new HashSet<string>();

            foreach (var obj in Selection.objects)
            {
                // 1) Nếu là asset path
                var path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        // Là thư mục: quét toàn bộ prefab đệ quy
                        foreach (var p in FindPrefabPathsUnder(path))
                            prefabAssetPaths.Add(p);
                    }
                    else
                    {
                        // Có thể là prefab asset đơn lẻ
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                            prefabAssetPaths.Add(path);
                    }
                }

                // Scene object
                if (obj is GameObject go)
                {
                    ProcessRoot(go);
                    EditorUtility.SetDirty(go);
                }
            }

            // Xử lý tất cả prefab assets đã gom
            foreach (var prefabPath in prefabAssetPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;

                ProcessRoot(prefab);
                EditorUtility.SetDirty(prefab);
            }

            AssetDatabase.SaveAssets();
        }
        // Tìm tất cả prefab dưới một (hoặc nhiều) thư mục (đệ quy)
        private static IEnumerable<string> FindPrefabPathsUnder(params string[] folders)
        {
            // Unity 2019+ hỗ trợ "t:Prefab"; nếu dự án cũ hơn, có thể dùng "t:GameObject"
            var guids = AssetDatabase.FindAssets("t:Prefab", folders);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                    yield return path;
            }
        }

        private static void ProcessRoot(GameObject root)
        {
            var tmps = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var tmp in tmps)
            {
                var binder = tmp.GetComponent<AutoI2TermBinder>();
                if (!binder)
                {
                    binder = Undo.AddComponent<AutoI2TermBinder>(tmp.gameObject);

                    if (tmp.gameObject.name.Contains("DoNotLocalize"))
                    {
                        // dynamic → kệ
                    }
                    else
                    {
                        AutoI2TermBinder.AutoI2TermBinderEditor.TryCreateAndPushTerm_API(binder, tmp);
                    }

                    EditorUtility.SetDirty(binder);
                    AssetDatabase.SaveAssets();
                }
            }

            EditorUtility.SetDirty(root);
            AssetDatabase.SaveAssets();
        }
    }
}
