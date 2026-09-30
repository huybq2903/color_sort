
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Translate.Editor
{
    public class PrefabFolderGuard : AssetModificationProcessor
    {
        // Folder cần bảo vệ!
        static string _protectedFolder = "Assets/Falcon/Modules/Translate/";

        static bool _guardEnabled = true;

        // Hook vào trước khi asset được save/lưu
        public static string[] OnWillSaveAssets(string[] paths)
        {
            if (!_guardEnabled)
                return paths;

            var deniedPaths = new System.Collections.Generic.List<string>();
            bool denied = false;

            foreach (var path in paths)
            {
                if (IsInProtectedFolder(path) && path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Nếu là mới tạo file thì cho phép (asset chưa từng tồn tại)
                    if (!AssetDatabase.LoadAssetAtPath<Object>(path))
                    {
                        continue; // File mới tạo, cho phép
                    }

                    // Còn lại là các thao tác chỉnh sửa/lưu/apply, thì CHẶN
                    denied = true;
                    deniedPaths.Add(path);
                    Debug.LogWarning($"Chặn lưu/chỉnh sửa prefab/variant trong folder bảo vệ: {path}");
                }
            }

            if (denied)
            {
                EditorUtility.DisplayDialog(
                    "Prefab trong module được bảo vệ",
                    $"KHÔNG ĐƯỢC PHÉP chỉnh sửa/lưu prefab hoặc variant trong thư mục:\n{_protectedFolder}\n" +
                    $"Bạn nên tạo variant, sau đó kéo ra ngoài và chỉnh sửa\nBạn cũng không thể apply vào prefab gốc nằm trong module.",
                    "OK"
                );
                // Chỉ trả về paths đã loại bỏ file bị block
                return RemovePaths(paths, deniedPaths);
            }

            return paths; // Nếu không có file bị block thì lưu như thường
        }

        static bool IsInProtectedFolder(string assetPath)
        {
            assetPath = assetPath.Replace("\\", "/");
            return assetPath.StartsWith(_protectedFolder, System.StringComparison.OrdinalIgnoreCase);
        }

        static string[] RemovePaths(string[] original, System.Collections.Generic.List<string> toRemove)
        {
            var list = new System.Collections.Generic.List<string>(original);
            foreach (var remove in toRemove)
                list.Remove(remove);
            return list.ToArray();
        }
    }
}
