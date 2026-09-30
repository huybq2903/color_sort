// Bỏ, không dùng nữa

// Falcon LSA Manager Window – Tabs: Base | Override (with I2 tab isolation)
// Menu: Falcon -> Modules -> LocalizationService -> LSA Manager Window
//
// - Hai TAB: 
//     * Override (Editable): cho phép sửa/ thêm term (ghi vào LSA Override)
//     * Base (Read‑only by policy): KHÔNG khoá UI để bạn vẫn cuộn/tra cứu, 
//       nhưng có cảnh báo lớn. (Chúng tôi không tự Save Base).
// - Fix: Mỗi lần vẽ tab, ép I2 inspector dùng đúng LanguageSourceData của asset đó
//        để tránh tình trạng "bật tab Override mà lại thấy Base".
// - Override auto-find trong BẤT KỲ thư mục Resources tên <BaseName> + "_Override".
//   Nếu không có, tự tạo fallback: đổi "Assets/Falcon" -> "Assets/FalconAssets", giữ subpath & Resources.
//
// Cách dùng: Falcon → Modules → LocalizationService → LSA Manager Window
//
// Yêu cầu: I2 Localization (namespace I2.Loc)

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using I2.Loc;

namespace Falcon.Modules.LocalizationService.Editor
{
    public class FalconLSAManagerWindow : EditorWindow
    {
        private enum ViewMode { ModuleSelect, Manage }
        private enum Tab { Override = 0, Base = 1 }

        private const string OverrideSuffix = "_Override";
        private const string PrefsKeyState = "FalconLSAManagerWindow.State";
        private const string PrefsKeyTab = "FalconLSAManagerWindow.Tab";

        [Serializable]
        private class State
        {
            public ViewMode Mode = ViewMode.ModuleSelect;
            public string SelectedModuleRoot = string.Empty;   // Assets/Falcon/Modules/<Module>
            public string SelectedBasePath = string.Empty;     // Base LSA path
            public string SelectedOverridePath = string.Empty; // Override path (found/created)
            public Vector2 ModuleScroll;
            public int ActiveTab = (int)Tab.Override;          // mặc định vào Override
        }

        private class ModuleInfo
        {
            public string PackageJsonPath;   // Assets/Falcon/Modules/<Module>/package.json
            public string ModuleRoot;        // dir of package.json
            public string ResourcesDir;      // <ModuleRoot>/Resources
            public List<string> BaseLSAPaths = new(); // all LSA under Resources
        }

        [SerializeField] private State _state = new State();
        private readonly List<ModuleInfo> _modules = new();

        // Cached assets + editors
        private LanguageSourceAsset _baseAsset;
        private LanguageSourceAsset _overrideAsset;
        private UnityEditor.Editor _baseEditor;
        private UnityEditor.Editor _overrideEditor;

        // I2 editor type cache
        private Type _i2EditorType;

        //[MenuItem("Falcon/Modules/LocalizationService/LSA Manager Window")]
        private static void Open()
        {
            var win = GetWindow<FalconLSAManagerWindow>("LSA Manager Window");
            win.minSize = new Vector2(900, 540);
            win.Show();
        }

        private void OnEnable()
        {
            try
            {
                var json = EditorPrefs.GetString(PrefsKeyState, string.Empty);
                if (!string.IsNullOrEmpty(json)) JsonUtility.FromJsonOverwrite(json, _state);
                _state.ActiveTab = EditorPrefs.GetInt(PrefsKeyTab, (int)Tab.Override);
            }
            catch { }

            if (_state.Mode == ViewMode.ModuleSelect) RefreshModules(); else LoadSelectedAssets();
        }

        private void OnDisable()
        {
            try
            {
                EditorPrefs.SetString(PrefsKeyState, JsonUtility.ToJson(_state));
                EditorPrefs.SetInt(PrefsKeyTab, _state.ActiveTab);
            }
            catch { }

            DisposeEditors();
        }

        private void OnGUI()
        {
            DrawTopBar();
            EditorGUILayout.Space();

            switch (_state.Mode)
            {
                case ViewMode.ModuleSelect: DrawModuleSelect(); break;
                case ViewMode.Manage: DrawManage(); break;
            }
        }

        private void DrawTopBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Rescan", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    RefreshModules();

                GUILayout.FlexibleSpace();

                if (_state.Mode == ViewMode.Manage)
                {
                    if (GUILayout.Button("Back to Modules", EditorStyles.toolbarButton, GUILayout.Width(130)))
                    {
                        _state.Mode = ViewMode.ModuleSelect;
                        _state.SelectedModuleRoot = _state.SelectedBasePath = _state.SelectedOverridePath = string.Empty;
                        _baseAsset = _overrideAsset = null;
                        DisposeEditors();
                    }
                }
            }
        }

        #region Module Select
        private void RefreshModules()
        {
            _modules.Clear();

            var guids = AssetDatabase.FindAssets("package t:TextAsset", new[] { "Assets/Falcon/Modules" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)) continue;

                var moduleRoot = Path.GetDirectoryName(path)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(moduleRoot)) continue;

                var resourcesDir = moduleRoot + "/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesDir)) continue;

                var lsaGuids = AssetDatabase.FindAssets("t:LanguageSourceAsset", new[] { resourcesDir });
                if (lsaGuids == null || lsaGuids.Length == 0) continue;

                var info = new ModuleInfo
                {
                    PackageJsonPath = path,
                    ModuleRoot = moduleRoot,
                    ResourcesDir = resourcesDir,
                    BaseLSAPaths = lsaGuids.Select(AssetDatabase.GUIDToAssetPath).ToList()
                };
                _modules.Add(info);
            }

            _modules.Sort((a, b) => string.CompareOrdinal(a.ModuleRoot, b.ModuleRoot));
        }

        private void DrawModuleSelect()
        {
            EditorGUILayout.LabelField("Chọn Module (có Base LSA)", EditorStyles.boldLabel);

            if (_modules.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Không tìm thấy module có Base LSA. Cấu trúc: Assets/Falcon/Modules/<Module>/package.json và Resources chứa LanguageSourceAsset.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _state.ModuleScroll = EditorGUILayout.BeginScrollView(_state.ModuleScroll, GUILayout.MinHeight(300));
                foreach (var m in _modules)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField($"Module: {Path.GetFileName(m.ModuleRoot)}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Root:", m.ModuleRoot);
                        EditorGUILayout.LabelField("Resources:", m.ResourcesDir);
                        EditorGUILayout.Space(2);

                        foreach (var basePath in m.BaseLSAPaths)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Base LSA:", basePath);

                                var foundOverride = FindOverrideAcrossResources(basePath);
                                var displayPath = string.IsNullOrEmpty(foundOverride)
                                    ? DeriveOverrideFallbackPath(basePath) + " (auto-create)"
                                    : foundOverride;

                                EditorGUILayout.LabelField("Override:", displayPath);
                                GUILayout.FlexibleSpace();

                                if (GUILayout.Button("Manage", GUILayout.Width(90)))
                                {
                                    _state.SelectedModuleRoot = m.ModuleRoot;
                                    _state.SelectedBasePath = basePath;
                                    _state.SelectedOverridePath = string.IsNullOrEmpty(foundOverride)
                                        ? DeriveOverrideFallbackPath(basePath)
                                        : foundOverride;
                                    _state.Mode = ViewMode.Manage;
                                    LoadSelectedAssets();
                                }
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }
        #endregion

        #region Manage (tabbed: Override | Base)
        private void LoadSelectedAssets()
        {
            _baseAsset = LoadLSA(_state.SelectedBasePath);

            var discovered = FindOverrideAcrossResources(_state.SelectedBasePath);
            _state.SelectedOverridePath = string.IsNullOrEmpty(discovered)
                ? DeriveOverrideFallbackPath(_state.SelectedBasePath)
                : discovered;

            _overrideAsset = EnsureOverrideAssetExists(_state.SelectedOverridePath);

            DisposeEditors(); // recreate editors for new selection
            _state.ActiveTab = (int)Tab.Override; // luôn mặc định về Override khi vào manage
        }

        private void DrawManage()
        {
            // Header thông tin & hướng dẫn
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Module:", _state.SelectedModuleRoot);
                EditorGUILayout.Space(2);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField("Base LSA (policy: don't edit)", _baseAsset, typeof(LanguageSourceAsset), false);
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField("Override LSA (edit here)", _overrideAsset, typeof(LanguageSourceAsset), false);
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox(
                    "📌 Tab 'Override (Editable)': Thêm/Sửa term tại đây. " +
                    "Ô trống = dùng giá trị từ Base.\n" +
                    "📎 Tab 'Base (Read‑only by policy)': Chỉ để xem, vui lòng không chỉnh ở đây.",
                    MessageType.Info);
            }

            // Tabs rõ ràng
            var tabLabels = new[] {
                new GUIContent("Override (Editable)"),
                new GUIContent("Base (Read‑only by policy)")
            };

            int prev = _state.ActiveTab;
            _state.ActiveTab = GUILayout.Toolbar(_state.ActiveTab, tabLabels, GUILayout.Height(24));
            if (_state.ActiveTab != prev)
            {
                // đổi tab -> clear editors để tránh cache sai
                DisposeEditors();
                Repaint();
            }

            EditorGUILayout.Space();

            // Nội dung theo tab
            if ((Tab)_state.ActiveTab == Tab.Override) DrawOverrideTab();
            else DrawBaseTab();
        }

        private void DrawBaseTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Base (Read‑only by policy)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Đây là LSA Base của module. Vui lòng KHÔNG chỉnh sửa ở đây. " +
                    "Nếu cần cập nhật bản dịch, hãy chuyển sang tab 'Override (Editable)'.",
                    MessageType.Warning);

                if (_baseAsset == null)
                {
                    EditorGUILayout.HelpBox("Base asset không tồn tại.", MessageType.Error);
                    return;
                }

                if (_baseEditor == null || _baseEditor.target != _baseAsset)
                    UnityEditor.Editor.CreateCachedEditor(_baseAsset, null, ref _baseEditor);

                // Ép I2 inspector nhìn đúng source trước khi vẽ
                ForceI2SelectedSource(_baseAsset);

                // Cho phép cuộn/tra cứu: KHÔNG khoá GUI
                _baseEditor.OnInspectorGUI();

                // Quan điểm "read-only by policy": KHÔNG tự SetDirty/Save cho Base
            }
        }

        private void DrawOverrideTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Override (Editable)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Đây là LSA Override. Bạn có thể thêm/sửa term tại đây. " +
                    "Ô trống nghĩa là dùng giá trị từ Base.",
                    MessageType.None);

                if (_overrideAsset == null)
                {
                    EditorGUILayout.HelpBox("Override asset không tồn tại.", MessageType.Error);
                    return;
                }

                if (_overrideEditor == null || _overrideEditor.target != _overrideAsset)
                    UnityEditor.Editor.CreateCachedEditor(_overrideAsset, null, ref _overrideEditor);

                // Ép I2 inspector nhìn đúng source trước khi vẽ
                ForceI2SelectedSource(_overrideAsset);

                EditorGUI.BeginChangeCheck();
                _overrideEditor.OnInspectorGUI(); // inspector gốc I2
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_overrideAsset);
                    AssetDatabase.SaveAssets();
                }
            }
        }
        #endregion

        #region I2 Helpers
        // Gán các field static trong I2 LocalizationEditor kiểu LanguageSourceData/Asset trỏ về asset đang vẽ
        private void ForceI2SelectedSource(LanguageSourceAsset asset)
        {
            if (asset == null) return;
            _i2EditorType ??= FindType("I2.Loc.LocalizationEditor");
            if (_i2EditorType == null) return;

            try
            {
                var fields = _i2EditorType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    var ft = f.FieldType;
                    if (ft == typeof(LanguageSourceData))
                    {
                        f.SetValue(null, asset.mSource);
                    }
                    else if (ft == typeof(LanguageSourceAsset))
                    {
                        f.SetValue(null, asset);
                    }
                }
            }
            catch { /* best effort */ }
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }
        #endregion

        #region Paths & loading
        // Tìm override: cùng tên = BaseName + "_Override" trong BẤT KỲ thư mục Resources
        private static string FindOverrideAcrossResources(string baseAssetPath)
        {
            if (string.IsNullOrEmpty(baseAssetPath)) return string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(baseAssetPath) + OverrideSuffix;

            var guids = AssetDatabase.FindAssets($"t:LanguageSourceAsset {baseName}");
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g).Replace('\\', '/');
                if (!p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;
                if (!p.Contains("/Resources/", StringComparison.Ordinal)) continue;
                if (Path.GetFileNameWithoutExtension(p).Equals(baseName, StringComparison.Ordinal))
                    return p;
            }
            return string.Empty;
        }

        // Fallback: cùng subpath với Base nhưng đổi prefix + thêm hậu tố
        private static string DeriveOverrideFallbackPath(string baseAssetPath)
        {
            if (string.IsNullOrEmpty(baseAssetPath)) return string.Empty;
            string path = baseAssetPath.Replace('\\', '/');

            const string from = "Assets/Falcon";
            const string to = "Assets/FalconAssets";
            if (path.StartsWith(from, StringComparison.Ordinal))
                path = to + path.Substring(from.Length);

            var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var file = Path.GetFileNameWithoutExtension(baseAssetPath) + OverrideSuffix + ".asset";
            return string.IsNullOrEmpty(dir) ? file : $"{dir}/{file}";
        }

        private static LanguageSourceAsset LoadLSA(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(path);
        }

        private LanguageSourceAsset EnsureOverrideAssetExists(string overridePath)
        {
            var a = LoadLSA(overridePath);
            if (a != null) return a;

            var dir = Path.GetDirectoryName(overridePath)?.Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir)) EnsureFolders(dir);

            var inst = ScriptableObject.CreateInstance<LanguageSourceAsset>();
            inst.mSource = new LanguageSourceData();
            AssetDatabase.CreateAsset(inst, overridePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            a = LoadLSA(overridePath);
            return a;
        }

        private static void EnsureFolders(string targetDir)
        {
            if (string.IsNullOrEmpty(targetDir)) return;
            var parts = targetDir.Split('/');
            var current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
        #endregion

        #region Dispose
        private void DisposeEditors()
        {
            if (_baseEditor != null) { DestroyImmediate(_baseEditor); _baseEditor = null; }
            if (_overrideEditor != null) { DestroyImmediate(_overrideEditor); _overrideEditor = null; }
        }
        #endregion
    }
}
#endif // UNITY_EDITOR
