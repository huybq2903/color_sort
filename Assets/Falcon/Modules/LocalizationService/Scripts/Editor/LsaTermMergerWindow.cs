// LSA Term Merger (I2 Localization)
// Adds all missing Terms from *all other* Language Source Assets (sources) into the selected Target LSA.
// - Window starts with only Target picker
// - Below shows a list of all other LSAs in the project (can temporarily Remove items in this session)
// - Button is enabled after Target is chosen and merges from the list into Target
// - No cross-session persistence
// Menu: Falcon/Modules/LocalizationService/LSA Term Merger

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using I2.Loc;

namespace Falcon.Modules.LocalizationService.Editor
{
    public class LsaTermMergerWindow : EditorWindow
    {
        private LanguageSourceAsset _target;
        private readonly List<LanguageSourceAsset> _others = new();
        private readonly HashSet<string> _excludedGuids = new(); // các LSA bị loại tạm thời trong phiên hiện tại
        private Vector2 _scroll;

        [MenuItem("Falcon/Modules/LocalizationService/LSA Term Merger")]
        public static void Open()
        {
            var wnd = GetWindow<LsaTermMergerWindow>(true, "LSA Term Merger", true);
            wnd.minSize = new Vector2(560, 360);
            wnd.Show();
        }

        private void OnEnable()
        {
            RefreshOtherLsas();
        }

        private void OnFocus()
        {
            // Refresh when window gains focus to reflect project changes; keep excluded entries for this session
            RefreshOtherLsas();
        }

        private void OnGUI()
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField("Merge Terms vào Target LSA", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Chọn Target LSA. Sau đó, tất cả LSA còn lại trong project sẽ hiện phía dưới (có thể Remove tạm thời).\n" +
                "Bấm nút để add toàn bộ term từ *các LSA bên dưới* vào Target (term đã có sẽ bỏ qua, translations copy theo tên ngôn ngữ).",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling))
            {
                EditorGUI.BeginChangeCheck();
                var newTarget = (LanguageSourceAsset)EditorGUILayout.ObjectField("Target LSA", _target, typeof(LanguageSourceAsset), false);
                if (EditorGUI.EndChangeCheck())
                {
                    _target = newTarget;
                    _excludedGuids.Clear();
                    RefreshOtherLsas();
                }

                GUILayout.Space(10);

                using (new EditorGUI.DisabledScope(_target == null || _others.Count == 0))
                {
                    if (GUILayout.Button("Add tất cả term của các LSA dưới đây vào Target LSA", GUILayout.Height(36)))
                    {
                        MergeAllIntoTarget();
                    }
                }

                GUILayout.Space(6);
                DrawOtherLsaList();
            }
        }

        private void DrawOtherLsaList()
        {
            EditorGUILayout.LabelField("Các LSA sẽ merge", EditorStyles.miniBoldLabel);
            if (_target == null)
            {
                EditorGUILayout.HelpBox("Chưa chọn Target LSA.", MessageType.Warning);
                return;
            }

            if (_others.Count == 0)
            {
                EditorGUILayout.HelpBox("Không có LSA nào để merge (đã bị loại hết, hoặc không tìm thấy).", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _others.Count; i++)
            {
                var lsa = _others[i];
                if (lsa == null) continue;

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(lsa.name, lsa, typeof(LanguageSourceAsset), false);
                }
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    var path = AssetDatabase.GetAssetPath(lsa);
                    var guid = string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
                    if (!string.IsNullOrEmpty(guid)) _excludedGuids.Add(guid);
                    _others.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Khôi phục danh sách", GUILayout.Width(160)))
                {
                    _excludedGuids.Clear();
                    RefreshOtherLsas();
                }
            }
        }

        private void RefreshOtherLsas()
        {
            _others.Clear();
            var guids = AssetDatabase.FindAssets("t:LanguageSourceAsset");
            foreach (var guid in guids)
            {
                if (_excludedGuids.Contains(guid)) continue; // bỏ qua các LSA đã remove tạm thời
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lsa = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(path);
                if (lsa == null) continue;
                if (_target != null && lsa == _target) continue; // exclude target
                _others.Add(lsa);
            }
            _others.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private void MergeAllIntoTarget()
        {
            if (_target == null || _others.Count == 0) return;

            int totalAdded = 0;
            int totalSkipped = 0;

            try
            {
                // Single undo entry for target changes
                Undo.RegisterCompleteObjectUndo(_target, "Merge Terms into LSA");

                int totalSources = _others.Count;
                for (int i = 0; i < totalSources; i++)
                {
                    var src = _others[i];
                    if (src == null || src.mSource == null) continue;

                    if (EditorUtility.DisplayCancelableProgressBar(
                        "Merging from Sources",
                        $"{src.name} ({i + 1}/{totalSources})",
                        (float)i / Mathf.Max(1, totalSources)))
                    {
                        break; // user cancelled
                    }

                    MergeFromSource(_target, src, out int added, out int skipped);
                    totalAdded += added;
                    totalSkipped += skipped;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.SetDirty(_target);
                AssetDatabase.SaveAssets();
            }

            EditorUtility.DisplayDialog(
                "LSA Term Merger",
                $"Hoàn tất. Added: {totalAdded}\nSkipped (đã tồn tại/không hợp lệ): {totalSkipped}",
                "OK");
        }

        private static void MergeFromSource(LanguageSourceAsset target, LanguageSourceAsset source, out int added, out int skipped)
        {
            added = 0;
            skipped = 0;

            var dstData = target.mSource;
            var srcData = source.mSource;
            if (dstData == null || srcData == null) return;

            var srcLangs = srcData.mLanguages?.Select(l => l.Name).ToArray() ?? System.Array.Empty<string>();
            var dstLangs = dstData.mLanguages?.Select(l => l.Name).ToArray() ?? System.Array.Empty<string>();

            var terms = srcData.mTerms?.ToArray() ?? System.Array.Empty<TermData>();

            for (int t = 0; t < terms.Length; t++)
            {
                var term = terms[t];
                if (term == null) { skipped++; continue; }

                var existing = dstData.GetTermData(term.Term);
                if (existing != null) { skipped++; continue; }

                var newTermData = dstData.AddTerm(term.Term, term.TermType);
                if (newTermData == null) { skipped++; continue; }

                newTermData.Description = term.Description;

                // Copy translations by matching language name
                for (int d = 0; d < dstLangs.Length; d++)
                {
                    int srcIndex = System.Array.IndexOf(srcLangs, dstLangs[d]);
                    if (srcIndex >= 0)
                    {
                        var val = term.GetTranslation(srcIndex);
                        if (!string.IsNullOrEmpty(val))
                        {
                            newTermData.SetTranslation(d, val);
                        }
                    }
                }

                added++;
            }
        }
    }
}