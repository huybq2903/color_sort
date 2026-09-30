using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using Falcon.Helpers.FPrefabUtilities.Runtime; // FakeApplyMarker

namespace Falcon.Helpers.FPrefabUtilities.Editor
{
    public static class AutoAttachFakeApplyMarker
    {
        #region Nested
        private const string MENU = "Falcon/Helper/FPrefabUtilities/Attach FakeApplyMarker (Only Nested Prefabs in Selected Folders)";

        [MenuItem(MENU)]
        private static void Run()
        {
            // Lấy danh sách thư mục đang chọn
            var selections = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            var folderPaths = selections
                .Select(AssetDatabase.GetAssetPath)
                .Where(AssetDatabase.IsValidFolder)
                .Select(Norm)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (folderPaths.Length == 0)
            {
                EditorUtility.DisplayDialog("Falcon", "Hãy chọn ít nhất 1 thư mục trong Project.", "OK");
                return;
            }

            // Tập các folder được chọn (điều kiện: nested asset thuộc BẤT KỲ folder nào trong đây)
            var selectedFolders = new HashSet<string>(
                folderPaths.Select(p => p.TrimEnd('/')),
                StringComparer.OrdinalIgnoreCase
            );

            // Báo cáo & thống kê
            int totalScannedPrefabs = 0;
            int totalModifiedAssets = 0;

            // Tránh mở/sửa lặp lại cùng một nested asset nhiều lần (khi gặp ở nhiều prefab cha)
            var alreadyModified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Thu thập chi tiết để UI gom nhóm: key = modified nested asset, value = set các parent prefabs đã “kích hoạt” việc gắn
            var modifiedToParents = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                for (int f = 0; f < folderPaths.Length; f++)
                {
                    string scanningFolder = folderPaths[f];

                    // Tìm tất cả prefab trong thư mục (đệ quy)
                    string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { scanningFolder });
                    totalScannedPrefabs += guids.Length;

                    for (int i = 0; i < guids.Length; i++)
                    {
                        string parentPrefabPath = Norm(AssetDatabase.GUIDToAssetPath(guids[i]));

                        EditorUtility.DisplayProgressBar(
                            "Falcon – Scan nested prefabs",
                            $"[{f + 1}/{folderPaths.Length}] {Path.GetFileName(parentPrefabPath)}",
                            (f + (i / Mathf.Max(1f, guids.Length))) / Mathf.Max(1f, folderPaths.Length)
                        );

                        // Mở nội dung prefab cha
                        var parentRoot = PrefabUtility.LoadPrefabContents(parentPrefabPath);
                        if (parentRoot == null)
                            continue;

                        try
                        {
                            // Duyệt toàn bộ cây (kể cả inactive), bỏ qua chính root của prefab cha
                            var transforms = parentRoot.GetComponentsInChildren<Transform>(true);
                            foreach (var t in transforms)
                            {
                                if (t == null) continue;
                                var go = t.gameObject;
                                if (go == parentRoot) continue;

                                // Xác định nested prefab instance root
                                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
                                if (!PrefabUtility.IsPartOfPrefabInstance(go)) continue;
                                if (PrefabUtility.GetPrefabInstanceStatus(go) != PrefabInstanceStatus.Connected) continue;

                                // Lấy đường dẫn prefab asset nguồn của instance
                                string nestedAssetPath = Norm(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go));
                                if (string.IsNullOrEmpty(nestedAssetPath)) continue;

                                // Điều kiện: nested asset nằm trong (các) folder được chọn
                                if (!IsUnderAnySelectedFolder(nestedAssetPath, selectedFolders)) continue;

                                // (Tuỳ chọn) Bỏ qua Model Prefab nếu không muốn sửa FBX/Model
                                // if (PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadAssetAtPath<GameObject>(nestedAssetPath)) == PrefabAssetType.Model)
                                //     continue;

                                // Mở prefab asset nguồn và gắn marker lên ROOT của nó (nếu chưa có)
                                // Tránh thao tác lặp vô ích nếu asset này đã được gắn trong lượt chạy này
                                if (alreadyModified.Contains(nestedAssetPath))
                                {
                                    // Vẫn ghi nhận parent kích hoạt (cho đẹp report)
                                    AddParentToReport(modifiedToParents, nestedAssetPath, parentPrefabPath);
                                    continue;
                                }

                                var nestedAssetRoot = PrefabUtility.LoadPrefabContents(nestedAssetPath);
                                if (nestedAssetRoot == null) continue;

                                bool modifiedThisAsset = false;
                                try
                                {
                                    if (nestedAssetRoot.GetComponent<FakeApplyMarker>() == null)
                                    {
                                        nestedAssetRoot.gameObject.AddComponent<FakeApplyMarker>();
                                        PrefabUtility.SaveAsPrefabAsset(nestedAssetRoot, nestedAssetPath);
                                        modifiedThisAsset = true;
                                        totalModifiedAssets++;
                                        alreadyModified.Add(nestedAssetPath);
                                    }

                                    // Dù có sửa hay không, vẫn thêm parent vào report (nếu đã sửa lần đầu)
                                    if (modifiedThisAsset)
                                        AddParentToReport(modifiedToParents, nestedAssetPath, parentPrefabPath);
                                }
                                finally
                                {
                                    PrefabUtility.UnloadPrefabContents(nestedAssetRoot);
                                }
                            }
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(parentRoot);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Falcon - Lỗi", $"Có lỗi xảy ra:\n{e.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // Chuẩn hoá dữ liệu report: mỗi asset được gắn mới -> 1 record với danh sách parent liên quan
            var report = modifiedToParents
                .Select(kv => new ReportItem
                {
                    ModifiedPrefabAssetPath = kv.Key,
                    TriggeredByParents = kv.Value?.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>()
                })
                .OrderBy(r => r.ModifiedPrefabAssetPath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            FalconMarkerReportWindow.Show(report, totalScannedPrefabs, totalModifiedAssets);
        }

        private static void AddParentToReport(Dictionary<string, HashSet<string>> map, string modifiedAssetPath, string parentPrefabPath)
        {
            if (!map.TryGetValue(modifiedAssetPath, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                map[modifiedAssetPath] = set;
            }
            set.Add(parentPrefabPath);
        }

        private static string Norm(string p) => p?.Replace("\\", "/");

        private static bool IsUnderAnySelectedFolder(string assetPath, IEnumerable<string> folders)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            assetPath = Norm(assetPath).TrimEnd('/');

            foreach (var folder in folders)
            {
                var ff = Norm(folder).TrimEnd('/');
                if (assetPath.StartsWith(ff + "/", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(assetPath, ff, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // Chặn click menu khi không có folder được chọn
        [MenuItem(MENU, true)]
        private static bool Validate()
        {
            var sels = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            return sels.Any(o => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(o)));
        }

        public class ReportItem
        {
            public string ModifiedPrefabAssetPath;     // Prefab nguồn vừa được gắn marker
            public List<string> TriggeredByParents;    // Những prefab cha có chứa instance dẫn đến việc gắn
        }
    }

    public class FalconMarkerReportWindow : EditorWindow
    {
        private List<AutoAttachFakeApplyMarker.ReportItem> _items;
        private Vector2 _scroll;
        private int _totalScannedPrefabs;
        private int _totalModifiedAssets;

        // UI state cho foldout theo asset
        private Dictionary<string, bool> _foldouts;

        public static void Show(List<AutoAttachFakeApplyMarker.ReportItem> items, int totalScannedPrefabs, int totalModifiedAssets)
        {
            var w = GetWindow<FalconMarkerReportWindow>("Falcon – FakeApplyMarker Report");
            w._items = items ?? new List<AutoAttachFakeApplyMarker.ReportItem>();
            w._totalScannedPrefabs = totalScannedPrefabs;
            w._totalModifiedAssets = totalModifiedAssets;
            w._foldouts = new Dictionary<string, bool>();
            w.minSize = new Vector2(680, 460);
            w.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Kết quả gắn FakeApplyMarker lên PREFAB NGUỒN (từ nested instances)", EditorStyles.boldLabel);
            GUILayout.Space(4);
            EditorGUILayout.LabelField("Tổng prefab đã quét", _totalScannedPrefabs.ToString());
            EditorGUILayout.LabelField("Số prefab nguồn đã gắn mới", _totalModifiedAssets.ToString());
            GUILayout.Space(8);

            if (_items == null || _items.Count == 0)
            {
                EditorGUILayout.HelpBox("Không có prefab nguồn nào được gắn mới. (Có thể tất cả đã có FakeApplyMarker từ trước).", MessageType.Info);
                return;
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                foreach (var it in _items)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        EditorGUILayout.LabelField("Prefab nguồn (đã gắn):", it.ModifiedPrefabAssetPath);

                        // Foldout: danh sách prefab cha có liên quan
                        if (!_foldouts.ContainsKey(it.ModifiedPrefabAssetPath))
                            _foldouts[it.ModifiedPrefabAssetPath] = false;

                        _foldouts[it.ModifiedPrefabAssetPath] = EditorGUILayout.Foldout(_foldouts[it.ModifiedPrefabAssetPath],
                            $"Prefab cha chứa nested instance này ({it.TriggeredByParents.Count})", true);

                        if (_foldouts[it.ModifiedPrefabAssetPath])
                        {
                            EditorGUI.indentLevel++;
                            foreach (var parent in it.TriggeredByParents)
                            {
                                EditorGUILayout.LabelField("- " + parent);
                            }
                            EditorGUI.indentLevel--;
                        }

                        GUILayout.Space(4);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Ping Prefab Nguồn", GUILayout.Width(160)))
                            {
                                var obj = AssetDatabase.LoadMainAssetAtPath(it.ModifiedPrefabAssetPath);
                                EditorGUIUtility.PingObject(obj);
                            }
                            if (GUILayout.Button("Mở Prefab Nguồn", GUILayout.Width(160)))
                            {
                                var obj = AssetDatabase.LoadMainAssetAtPath(it.ModifiedPrefabAssetPath);
                                AssetDatabase.OpenAsset(obj);
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region All
    public static partial class AutoAttachFakeApplyMarker_Simple
    {
        private const string MENU_SIMPLE = "Falcon/Helper/FPrefabUtilities/Attach FakeApplyMarker (All Prefabs in Selected Folders)";

        [MenuItem(MENU_SIMPLE)]
        private static void Run_SimpleAttachAll()
        {
            // Lấy các folder được chọn
            var selections = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            var folderPaths = selections
                .Select(AssetDatabase.GetAssetPath)
                .Where(AssetDatabase.IsValidFolder)
                .Select(p => p.Replace("\\", "/"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (folderPaths.Length == 0)
            {
                EditorUtility.DisplayDialog("Falcon", "Hãy chọn ít nhất 1 thư mục trong Project.", "OK");
                return;
            }

            int totalScanned = 0;
            int totalModified = 0;

            try
            {
                for (int f = 0; f < folderPaths.Length; f++)
                {
                    string scanningFolder = folderPaths[f];
                    string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { scanningFolder });
                    totalScanned += guids.Length;

                    for (int i = 0; i < guids.Length; i++)
                    {
                        string prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]).Replace("\\", "/");

                        EditorUtility.DisplayProgressBar(
                            "Falcon – Simple Attach",
                            $"[{f + 1}/{folderPaths.Length}] {System.IO.Path.GetFileName(prefabPath)}",
                            (f + (i / Mathf.Max(1f, guids.Length))) / Mathf.Max(1f, folderPaths.Length)
                        );

                        var root = PrefabUtility.LoadPrefabContents(prefabPath);
                        if (root == null) continue;

                        try
                        {
                            if (root.GetComponent<FakeApplyMarker>() == null)
                            {
                                root.gameObject.AddComponent<FakeApplyMarker>();
                                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                                totalModified++;
                                Debug.Log($"[Falcon] Added FakeApplyMarker → {prefabPath}");
                            }
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(root);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Falcon - Lỗi", $"Có lỗi xảy ra:\n{e.Message}", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog(
                "Falcon – Simple Attach",
                $"Đã quét {totalScanned} prefab.\n" +
                $"Đã gắn mới FakeApplyMarker cho {totalModified} prefab.",
                "OK"
            );
        }

        // Chỉ enable menu khi có folder được chọn
        [MenuItem(MENU_SIMPLE, true)]
        private static bool Validate_Simple()
        {
            var sels = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets);
            return sels.Any(o => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(o)));
        }
    }
    #endregion
}