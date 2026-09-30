#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace I2.Loc
{
    public class I2FontCharacterExtractorWindow : EditorWindow
    {
        private List<LanguageSourceAsset> _sourceAssets = new();
        private List<LanguageInfo> _allLanguages = new();
        private int _selectedLanguageIndex;
        private string _extractedChars = "";
        private string _newCharsOnly = "";
        private Vector2 _scrollExtracted;
        private Vector2 _scrollNew;
        private bool _foldoutSources = true;
        private TMP_FontAsset _targetFontAsset;
        private List<TMP_FontAsset> _projectFontAssets = new();
        private string[] _fontAssetNames = Array.Empty<string>();
        private int _selectedFontAssetIndex = -1;
        private float _savedLineHeight;
        private bool _hasLineHeightBackup;

        struct LanguageInfo
        {
            public string Name;
            public string Code;
            public string Display;
        }

        [MenuItem("Falcon/Tools/Font TMP Character Extractor")]
        public static void Open()
        {
            var wnd = GetWindow<I2FontCharacterExtractorWindow>(false, "I2 Font Char Extractor");
            wnd.minSize = new Vector2(480, 520);
            wnd.Show();
        }

        private void OnEnable()
        {
            RefreshSources();
        }

        private void OnFocus()
        {
            RefreshSources();
        }

        private void RefreshSources()
        {
            RefreshFontAssets();
            _sourceAssets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:LanguageSourceAsset", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(path);
                if (asset != null)
                    _sourceAssets.Add(asset);
            }
            RebuildLanguageList();
        }

        private void RebuildLanguageList()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _allLanguages.Clear();

            foreach (var asset in _sourceAssets)
            {
                if (asset?.SourceData?.mLanguages == null) continue;
                foreach (var lang in asset.SourceData.mLanguages)
                {
                    string key = lang.Code ?? lang.Name;
                    if (seen.Add(key))
                    {
                        _allLanguages.Add(new LanguageInfo
                        {
                            Name = lang.Name,
                            Code = lang.Code,
                            Display = $"{lang.Name} [{lang.Code}]"
                        });
                    }
                }
            }

            if (_selectedLanguageIndex >= _allLanguages.Count)
                _selectedLanguageIndex = 0;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("I2 Font Character Extractor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Trích xuất ký tự từ I2 Localization theo ngôn ngữ.\n" +
                "Chọn ngôn ngữ → Extract → Add vào TMP Font Asset Creator (Custom Characters).",
                MessageType.Info);

            EditorGUILayout.Space(6);

            _foldoutSources = EditorGUILayout.Foldout(_foldoutSources, $"Language Source Assets ({_sourceAssets.Count})", true);
            if (_foldoutSources)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < _sourceAssets.Count; i++)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(_sourceAssets[i], typeof(LanguageSourceAsset), false);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);

            if (_allLanguages.Count == 0)
            {
                EditorGUILayout.HelpBox("Không tìm thấy ngôn ngữ nào trong các LanguageSourceAsset.", MessageType.Warning);
                return;
            }

            string[] displayNames = _allLanguages.Select(l => l.Display).ToArray();
            _selectedLanguageIndex = EditorGUILayout.Popup("Ngôn ngữ", _selectedLanguageIndex, displayNames);

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Extract Characters", GUILayout.Height(30)))
            {
                ExtractCharacters();
            }

            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField($"Ký tự trích xuất ({_extractedChars.Length} chars)", EditorStyles.boldLabel);
            _scrollExtracted = EditorGUILayout.BeginScrollView(_scrollExtracted, GUILayout.Height(100));
            EditorGUILayout.TextArea(_extractedChars, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Copy Extracted to Clipboard"))
            {
                GUIUtility.systemCopyBuffer = _extractedChars;
                Debug.Log($"[I2FontCharExtractor] Copied {_extractedChars.Length} chars to clipboard.");
            }

            EditorGUILayout.Space(8);
            DrawTMPIntegration();
        }

        private void RefreshFontAssets()
        {
            _projectFontAssets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fa != null)
                    _projectFontAssets.Add(fa);
            }
            _fontAssetNames = _projectFontAssets.Select(f => f.name).ToArray();

            if (_targetFontAsset != null)
                _selectedFontAssetIndex = _projectFontAssets.IndexOf(_targetFontAsset);
            else
                _selectedFontAssetIndex = -1;
        }

        private void DrawTMPIntegration()
        {
            EditorGUILayout.LabelField("TMP Font Asset", EditorStyles.boldLabel);

            if (_projectFontAssets.Count == 0)
            {
                EditorGUILayout.HelpBox("Không tìm thấy TMP_FontAsset nào trong project.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            _selectedFontAssetIndex = EditorGUILayout.Popup("Target Font Asset", _selectedFontAssetIndex, _fontAssetNames);
            if (EditorGUI.EndChangeCheck())
            {
                _targetFontAsset = _selectedFontAssetIndex >= 0 ? _projectFontAssets[_selectedFontAssetIndex] : null;
                BackupLineHeight();
                ComputeNewChars();
            }

            if (_targetFontAsset != null)
            {
                int existingCount = _targetFontAsset.characterTable?.Count ?? 0;
                EditorGUILayout.LabelField($"Font Asset chars: {existingCount}");

                float currentLineHeight = _targetFontAsset.faceInfo.lineHeight;
                EditorGUILayout.LabelField($"Current Line Height: {currentLineHeight:F2}");
                if (_hasLineHeightBackup)
                {
                    EditorGUILayout.LabelField($"Saved Line Height: {_savedLineHeight:F2}");
                    bool changed = !Mathf.Approximately(currentLineHeight, _savedLineHeight);
                    using (new EditorGUI.DisabledScope(!changed))
                    {
                        var prevBg = GUI.backgroundColor;
                        if (changed) GUI.backgroundColor = Color.green;
                        if (GUILayout.Button(changed ? "Restore Original Line Height" : "Line Height OK"))
                        {
                            RestoreLineHeight();
                        }
                        GUI.backgroundColor = prevBg;
                    }
                }
            }

            EditorGUILayout.LabelField($"Ký tự mới (chưa có trong Font Asset): {_newCharsOnly.Length} chars", EditorStyles.boldLabel);
            _scrollNew = EditorGUILayout.BeginScrollView(_scrollNew, GUILayout.Height(80));
            EditorGUILayout.TextArea(_newCharsOnly, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Copy New Characters to Clipboard"))
            {
                GUIUtility.systemCopyBuffer = _newCharsOnly;
                Debug.Log($"[I2FontCharExtractor] Copied {_newCharsOnly.Length} new chars to clipboard.");
            }

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(_targetFontAsset == null || string.IsNullOrEmpty(_newCharsOnly)))
            {
                if (GUILayout.Button("Confirm: Add New Characters to Font Asset Creator", GUILayout.Height(30)))
                {
                    OpenTMPCreatorAndAppendChars();
                }
            }
        }

        private void ExtractCharacters()
        {
            if (_selectedLanguageIndex < 0 || _selectedLanguageIndex >= _allLanguages.Count)
                return;

            var targetLang = _allLanguages[_selectedLanguageIndex];
            var charSet = new HashSet<char>();

            foreach (var asset in _sourceAssets)
            {
                if (asset?.SourceData == null) continue;
                var src = asset.SourceData;

                int langIdx = -1;
                for (int i = 0; i < src.mLanguages.Count; i++)
                {
                    if (string.Equals(src.mLanguages[i].Code, targetLang.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        langIdx = i;
                        break;
                    }
                }
                if (langIdx < 0) continue;

                foreach (var term in src.mTerms)
                {
                    if (term.TermType != eTermType.Text) continue;
                    if (term.Languages == null || langIdx >= term.Languages.Length) continue;

                    string translation = term.Languages[langIdx];
                    if (string.IsNullOrEmpty(translation) || translation == "---") continue;

                    translation = CleanI2Tags(translation);

                    foreach (char c in translation)
                    {
                        if (!char.IsControl(c))
                            charSet.Add(c);
                    }
                }
            }

            var expanded = new HashSet<char>(charSet);
            foreach (char c in charSet)
            {
                if (char.IsLetter(c))
                {
                    expanded.Add(char.ToUpperInvariant(c));
                    expanded.Add(char.ToLowerInvariant(c));
                }
            }

            var sorted = expanded.OrderBy(c => c).ToArray();
            _extractedChars = new string(sorted);

            ComputeNewChars();

            Debug.Log($"[I2FontCharExtractor] Extracted {_extractedChars.Length} unique chars for [{targetLang.Code}] {targetLang.Name}");
        }

        private static string CleanI2Tags(string text)
        {
            text = text.Replace("[i2nt]", "").Replace("[/i2nt]", "");

            int idx;
            while ((idx = text.IndexOf("[i2s_", StringComparison.Ordinal)) >= 0)
            {
                int end = text.IndexOf(']', idx);
                if (end < 0) break;
                text = text.Remove(idx, end - idx + 1);
            }
            while ((idx = text.IndexOf("[/i2s_", StringComparison.Ordinal)) >= 0)
            {
                int end = text.IndexOf(']', idx);
                if (end < 0) break;
                text = text.Remove(idx, end - idx + 1);
            }

            return text;
        }

        private void BackupLineHeight()
        {
            if (_targetFontAsset == null)
            {
                _hasLineHeightBackup = false;
                return;
            }
            _savedLineHeight = _targetFontAsset.faceInfo.lineHeight;
            _hasLineHeightBackup = true;
        }

        private void RestoreLineHeight()
        {
            if (_targetFontAsset == null || !_hasLineHeightBackup) return;
            var faceInfo = _targetFontAsset.faceInfo;
            faceInfo.lineHeight = _savedLineHeight;
            _targetFontAsset.faceInfo = faceInfo;
            EditorUtility.SetDirty(_targetFontAsset);
            Debug.Log($"[I2FontCharExtractor] Restored line height to {_savedLineHeight:F2} for [{_targetFontAsset.name}]");
        }

        private void ComputeNewChars()
        {
            if (string.IsNullOrEmpty(_extractedChars))
            {
                _newCharsOnly = "";
                return;
            }

            var existing = new HashSet<uint>();
            if (_targetFontAsset != null && _targetFontAsset.characterTable != null)
            {
                foreach (var ch in _targetFontAsset.characterTable)
                    existing.Add(ch.unicode);
            }

            var sb = new StringBuilder();
            foreach (char c in _extractedChars)
            {
                if (!existing.Contains(c))
                    sb.Append(c);
            }
            _newCharsOnly = sb.ToString();
        }

        private void OpenTMPCreatorAndAppendChars()
        {
            if (_targetFontAsset == null || string.IsNullOrEmpty(_newCharsOnly)) return;

            var creatorType = GetTMPCreatorType();
            if (creatorType == null)
            {
                Debug.LogError("[I2FontCharExtractor] Cannot find TMPro_FontAssetCreatorWindow type.");
                return;
            }

            var showMethod = creatorType.GetMethod("ShowFontAtlasCreatorWindow",
                BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(TMP_FontAsset) }, null);

            if (showMethod != null)
            {
                showMethod.Invoke(null, new object[] { _targetFontAsset });
            }
            else
            {
                EditorWindow.GetWindow(creatorType);
            }

            var wnd = EditorWindow.GetWindow(creatorType, false, null, false);
            if (wnd == null) return;

            var fieldMode = creatorType.GetField("m_CharacterSetSelectionMode",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var fieldSeq = creatorType.GetField("m_CharacterSequence",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (fieldMode == null || fieldSeq == null)
            {
                Debug.LogError("[I2FontCharExtractor] Cannot access TMP Creator internal fields.");
                return;
            }

            fieldMode.SetValue(wnd, 7);

            string existingSeq = (fieldSeq.GetValue(wnd) as string) ?? "";
            var existingChars = new HashSet<char>(existingSeq);
            var sb = new StringBuilder(existingSeq);

            int addedCount = 0;
            foreach (char c in _newCharsOnly)
            {
                if (existingChars.Add(c))
                {
                    sb.Append(c);
                    addedCount++;
                }
            }

            fieldSeq.SetValue(wnd, sb.ToString());
            wnd.Repaint();

            Debug.Log($"[I2FontCharExtractor] Opened Font Asset Creator for [{_targetFontAsset.name}] and added {addedCount} new characters. Total custom list: {sb.Length}");
        }

        private static Type GetTMPCreatorType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("TMPro.EditorUtilities.TMPro_FontAssetCreatorWindow");
                if (t != null) return t;
            }
            return null;
        }
    }
}
#endif
