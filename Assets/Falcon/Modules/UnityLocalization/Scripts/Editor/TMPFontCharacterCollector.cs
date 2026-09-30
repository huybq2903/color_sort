using UnityEngine;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using I2.Loc;
using UnityEngine.TextCore;

namespace Falcon.Modules.UnityLocalization.Editor
{
    /// <summary>
    /// Tool tự động thu thập tất cả ký tự từ I2 Localization và Unity Localization
    /// để tạo character set cho Static TMP Font
    /// Hỗ trợ lọc theo ngôn ngữ để tối ưu từng font riêng biệt
    /// </summary>
    public class TMPFontCharacterCollector : EditorWindow
    {
        private Vector2 mainScrollPos;
        private const float LanguageColumnWidth = 140f;
        private Vector2 resultsScrollPos;
        private string collectedCharacters = "";
        private int totalCharacters;
        private int uniqueCharacters;
        private Dictionary<string, int> languageStats = new Dictionary<string, int>();
        private bool hasCollected;
        private TMP_FontAsset targetFont;
        private bool includeI2Localization = true;
        private bool includeUnityLocalization = true;
        private bool includeNumbers = true;
        private bool includePunctuation = true;
        private bool includeSpecialChars = true;
        private bool includeLetters = true;
        private bool autoSizePointSize = true;

        // Language filtering
        private bool showLanguageFilter;
        private Dictionary<string, bool> selectedLanguages = new Dictionary<string, bool>();
        private List<string> availableLanguages = new List<string>();
        private bool hasScannedLanguages;

        // GUI folding state
        private bool showOptions = true;
        private bool showResults = true;

        [MenuItem("Falcon/Modules/Unity Localization/Character Collector")]
        public static void ShowWindow()
        {
            var window = GetWindow<TMPFontCharacterCollector>("Character Collector");
            window.minSize = new Vector2(700, 650);
            window.Show();
        }

        private void OnGUI()
        {
            // Header (fixed)
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("TMP Font Character Collector", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Thu thập ký tự từ Localization + Lọc theo ngôn ngữ", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            // Actions (fixed at top for easy access)
            DrawActions();

            EditorGUILayout.Space(5);

            // Main scrollable content
            mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

            EditorGUILayout.HelpBox(
                "1. Scan Languages → 2. Chọn ngôn ngữ → 3. Thu thập → 4. Update Font",
                MessageType.Info);

            EditorGUILayout.Space(5);

            // Options
            DrawOptions();

            if (hasCollected)
            {
                EditorGUILayout.Space(10);
                DrawResults();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showOptions = EditorGUILayout.Foldout(showOptions, "⚙️ Tùy Chọn (Options)", true, EditorStyles.foldoutHeader);

            if (showOptions)
            {
                EditorGUILayout.Space(3);

                // Target Font (most important - show first)
                EditorGUILayout.LabelField("Target Font:", EditorStyles.boldLabel);
                targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(targetFont, typeof(TMP_FontAsset), false);

                if (targetFont != null)
                {
                    Color origColor = GUI.backgroundColor;
                    GUI.backgroundColor = targetFont.atlasPopulationMode == AtlasPopulationMode.Static ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
                    EditorGUILayout.HelpBox(
                        $"• {targetFont.name}\n" +
                        $"• Mode: {targetFont.atlasPopulationMode}\n" +
                        $"• Characters: {targetFont.characterTable?.Count ?? 0}",
                        targetFont.atlasPopulationMode == AtlasPopulationMode.Static ? MessageType.Info : MessageType.Warning);
                    GUI.backgroundColor = origColor;
                }

                EditorGUILayout.Space(5);

                // Localization sources
                EditorGUILayout.BeginHorizontal();
                includeI2Localization = EditorGUILayout.ToggleLeft("I2 Localization", includeI2Localization, GUILayout.Width(140));
                includeUnityLocalization = EditorGUILayout.ToggleLeft("Unity Localization", includeUnityLocalization);
                EditorGUILayout.EndHorizontal();

                // Additional characters (compact in one row)
                EditorGUILayout.BeginHorizontal();
                includeNumbers = EditorGUILayout.ToggleLeft("0-9", includeNumbers, GUILayout.Width(60));
                includePunctuation = EditorGUILayout.ToggleLeft(".,!?", includePunctuation, GUILayout.Width(60));
                includeSpecialChars = EditorGUILayout.ToggleLeft("$€¥", includeSpecialChars, GUILayout.Width(60));
                includeLetters = EditorGUILayout.ToggleLeft("A-Z", includeLetters);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);
                autoSizePointSize = EditorGUILayout.ToggleLeft(
                    $"Auto-size point size (nhồi to nhất còn vừa 1 texture {AtlasSize})", autoSizePointSize);
            }

            EditorGUILayout.EndVertical();

            // Language Filter Section
            DrawLanguageFilter();
        }

        private void DrawLanguageFilter()
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            showLanguageFilter = EditorGUILayout.Foldout(showLanguageFilter, "🌐 Lọc Ngôn Ngữ (Language Filter)", true, EditorStyles.foldoutHeader);

            if (GUILayout.Button(hasScannedLanguages ? "Rescan" : "Scan Languages", GUILayout.Width(100)))
            {
                ScanAvailableLanguages();
            }
            EditorGUILayout.EndHorizontal();

            if (showLanguageFilter)
            {
                if (!hasScannedLanguages)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("💡 Latin font: EN+VI | Chinese: CN | Japanese: JP | Korean: KR", EditorStyles.miniLabel);
                }
                else if (availableLanguages.Count == 0)
                {
                    EditorGUILayout.HelpBox("Không tìm thấy ngôn ngữ.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.Space(3);

                    // Summary bar
                    EditorGUILayout.BeginHorizontal();
                    int selectedCount = selectedLanguages.Count(kvp => kvp.Value);
                    EditorGUILayout.LabelField($"Đã chọn: {selectedCount}/{availableLanguages.Count}", EditorStyles.miniLabel);

                    if (GUILayout.Button("All", GUILayout.Width(40)))
                    {
                        foreach (var lang in availableLanguages)
                            selectedLanguages[lang] = true;
                    }
                    if (GUILayout.Button("None", GUILayout.Width(45)))
                    {
                        foreach (var lang in availableLanguages)
                            selectedLanguages[lang] = false;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(2);

                    // Grid: số cột theo bề rộng cửa sổ, hiện hết ngôn ngữ nên không cần scroll
                    int columns = Mathf.Max(1,
                        Mathf.FloorToInt(Mathf.Max(EditorGUIUtility.currentViewWidth - 40f, LanguageColumnWidth) / LanguageColumnWidth));

                    for (int i = 0; i < availableLanguages.Count; i++)
                    {
                        if (i % columns == 0) EditorGUILayout.BeginHorizontal();

                        var language = availableLanguages[i];
                        if (!selectedLanguages.ContainsKey(language))
                            selectedLanguages[language] = true;

                        bool wasSelected = selectedLanguages[language];
                        bool isSelected = EditorGUILayout.ToggleLeft(language, wasSelected, GUILayout.Width(LanguageColumnWidth));

                        if (isSelected != wasSelected)
                        {
                            selectedLanguages[language] = isSelected;
                        }

                        if (i % columns == columns - 1 || i == availableLanguages.Count - 1)
                            EditorGUILayout.EndHorizontal();
                    }

                    // Show selected preview (compact)
                    if (selectedCount > 0 && selectedCount < availableLanguages.Count)
                    {
                        var selectedNames = selectedLanguages.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
                        string preview = string.Join(", ", selectedNames.Take(4).ToArray());
                        if (selectedCount > 4) preview += $" +{selectedCount - 4}";
                        EditorGUILayout.LabelField($"→ {preview}", EditorStyles.miniLabel);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void ScanAvailableLanguages()
        {
            availableLanguages.Clear();
            selectedLanguages.Clear();

            // I2 và Unity đặt tên khác nhau cho cùng ngôn ngữ ("Portuguese (Portugal)" vs "Portuguese") nên
            // gom theo tên chuẩn I2; tên hiển thị lấy cái gặp trước (I2 quét trước).
            var byCanonical = new Dictionary<string, string>();

            void AddLanguage(string name)
            {
                if (string.IsNullOrEmpty(name)) return;
                var canonical = LocalizationManager.GetSupportedLanguage(name);
                if (string.IsNullOrEmpty(canonical)) canonical = name;
                if (!byCanonical.ContainsKey(canonical)) byCanonical[canonical] = name;
            }

            try
            {
                // Scan I2 Localization
                if (includeI2Localization)
                {
                    LocalizationManager.UpdateSources();
                    var sources = LocalizationManager.Sources;

                    if (sources != null && sources.Count > 0)
                    {
                        foreach (var source in sources)
                        {
                            if (source == null) continue;

                            foreach (var lang in source.mLanguages)
                                AddLanguage(lang.Name);
                        }
                    }
                }

                // Scan Unity Localization — dùng API editor (đọc asset trực tiếp, không cần init async).
                if (includeUnityLocalization)
                {
                    try
                    {
                        foreach (var locale in LocalizationEditorSettings.GetLocales())
                            if (locale != null) AddLanguage(locale.LocaleName);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[Character Collector] Unity Localization scan: {ex.Message}");
                    }
                }

                availableLanguages = byCanonical.Values.OrderBy(l => l).ToList();

                // Default: select all languages
                foreach (var lang in availableLanguages)
                {
                    selectedLanguages[lang] = true;
                }

                hasScannedLanguages = true;

                Debug.Log($"[Character Collector] Tìm thấy {availableLanguages.Count} ngôn ngữ: {string.Join(", ", availableLanguages.ToArray())}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Character Collector] Lỗi khi scan languages: {ex.Message}");
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Main actions row
            EditorGUILayout.BeginHorizontal();

            // Primary action
            Color origBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            if (GUILayout.Button("🔍 Thu Thập Ký Tự", GUILayout.Height(32)))
            {
                CollectCharacters();
            }
            GUI.backgroundColor = origBg;

            // Secondary actions
            GUI.enabled = hasCollected && !string.IsNullOrEmpty(collectedCharacters);
            if (GUILayout.Button("📋 Copy", GUILayout.Height(32), GUILayout.Width(70)))
            {
                EditorGUIUtility.systemCopyBuffer = collectedCharacters;
                Debug.Log($"[Character Collector] Đã copy {uniqueCharacters} ký tự!");
                ShowNotification(new GUIContent("✓ Copied!"));
            }
            GUI.enabled = true;

            GUI.enabled = hasCollected && targetFont != null && targetFont.atlasPopulationMode == AtlasPopulationMode.Static;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("⚡ Update Font Atlas", GUILayout.Height(32)))
            {
                UpdateFontAtlas();
            }
            GUI.backgroundColor = origBg;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // Warning if font not static
            if (targetFont != null && targetFont.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                EditorGUILayout.HelpBox("⚠️ Font phải ở Static mode!", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawResults()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showResults = EditorGUILayout.Foldout(showResults, $"📊 Kết Quả: {uniqueCharacters} ký tự unique", true, EditorStyles.foldoutHeader);

            if (showResults)
            {
                EditorGUILayout.Space(3);

                // Summary stats (compact)
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Unique: {uniqueCharacters}", EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField($"Total: {totalCharacters}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                // Language stats (collapsible)
                if (languageStats.Count > 0)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("Per Language:", EditorStyles.miniLabel);

                    // Show top 5, rest in scroll if needed
                    var sortedStats = languageStats.OrderByDescending(x => x.Value).ToList();
                    int showCount = Mathf.Min(5, sortedStats.Count);

                    for (int i = 0; i < showCount; i++)
                    {
                        var kvp = sortedStats[i];
                        EditorGUILayout.LabelField($"  • {kvp.Key}: {kvp.Value}", EditorStyles.miniLabel);
                    }

                    if (sortedStats.Count > 5)
                    {
                        EditorGUILayout.LabelField($"  ... +{sortedStats.Count - 5} more", EditorStyles.miniLabel);
                    }
                }

                EditorGUILayout.Space(5);

                // Character preview (fixed height scroll)
                EditorGUILayout.LabelField("Character Set Preview:", EditorStyles.miniLabel);

                resultsScrollPos = EditorGUILayout.BeginScrollView(resultsScrollPos, GUILayout.Height(120));

                GUIStyle textStyle = new GUIStyle(EditorStyles.textArea);
                textStyle.wordWrap = true;
                EditorGUILayout.TextArea(collectedCharacters, textStyle, GUILayout.ExpandHeight(true));

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("💡 Copy → Font Asset Creator → Custom Characters → Paste → Generate", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void CollectCharacters()
        {
            HashSet<char> allChars = new HashSet<char>();
            languageStats.Clear();
            totalCharacters = 0;

            Debug.Log("[Character Collector] Bắt đầu thu thập ký tự...");

            // If language filter is enabled and no languages selected, warn user
            if (hasScannedLanguages && showLanguageFilter)
            {
                int selectedCount = selectedLanguages.Count(kvp => kvp.Value);
                if (selectedCount == 0)
                {
                    EditorUtility.DisplayDialog("Warning",
                        "Không có ngôn ngữ nào được chọn!\n\n" +
                        "Vui lòng chọn ít nhất 1 ngôn ngữ hoặc tắt Language Filter.",
                        "OK");
                    return;
                }

                string selectedNames = string.Join(", ", selectedLanguages.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray());
                Debug.Log($"[Character Collector] Thu thập từ {selectedCount} ngôn ngữ: {selectedNames}");
            }

            // Collect from I2 Localization
            if (includeI2Localization)
            {
                CollectFromI2Localization(allChars);
            }

            // Collect from Unity Localization
            if (includeUnityLocalization)
            {
                CollectFromUnityLocalization(allChars);
            }

            // Add common characters
            if (includeNumbers)
            {
                AddCharacters(allChars, "0123456789");
            }
            else
            {
                RemoveCharacters(allChars, "0123456789");
            }

            if (includePunctuation)
            {
                AddCharacters(allChars, ".,!?;:'\"-()[]{}/<>\\|@#%&*+=_~`");
            }
            else
            {
                RemoveCharacters(allChars, ".,!?;:'\"-()[]{}/<>\\|@#%&*+=_~`");
            }

            if (includeSpecialChars)
            {
                AddCharacters(allChars, "$€£¥₫₽₩฿₪₹₨°©®™§¶†‡");
            }
            else
            {
                RemoveCharacters(allChars, "$€£¥₫₽₩฿₪₹₨°©®™§¶†‡");
            }

            if (!includeLetters)
            {
                foreach (var c in allChars.ToList())
                {
                    if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
                    {
                        allChars.Remove(c);
                    }
                }
            }

            // Always include space and common whitespace
            AddCharacters(allChars, " \t\n\r");

            // ✅ AUTO ADD UPPERCASE for Latin characters
            var charsToAddUppercase = new HashSet<char>(allChars);
            foreach (char c in charsToAddUppercase)
            {
                // Check if it's a letter (Latin, Vietnamese, European, etc.)
                if (char.IsLetter(c))
                {
                    // Add uppercase variant
                    char upperChar = char.ToUpperInvariant(c);
                    if (upperChar != c) // Only add if different
                    {
                        allChars.Add(upperChar);
                    }

                    // Add lowercase variant (in case we got uppercase first)
                    char lowerChar = char.ToLowerInvariant(c);
                    if (lowerChar != c)
                    {
                        allChars.Add(lowerChar);
                    }
                }
            }

            // Sort and create string
            var sortedChars = allChars.OrderBy(c => c).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (char c in sortedChars)
            {
                sb.Append(c);
            }

            collectedCharacters = sb.ToString();
            uniqueCharacters = allChars.Count;
            hasCollected = true;

            Debug.Log($"[Character Collector] Hoàn tất! Thu thập được {uniqueCharacters} ký tự unique (bao gồm cả uppercase/lowercase).");
        }

        private void CollectFromI2Localization(HashSet<char> allChars)
        {
            try
            {
                LocalizationManager.UpdateSources();

                var sources = LocalizationManager.Sources;
                if (sources == null || sources.Count == 0)
                {
                    Debug.LogWarning("[Character Collector] Không tìm thấy I2 Language Sources!");
                    return;
                }

                Debug.Log($"[Character Collector] Tìm thấy {sources.Count} I2 Language Sources");

                foreach (var source in sources)
                {
                    if (source == null) continue;

                    var languages = source.mLanguages;
                    var terms = source.mTerms;

                    string sourceName = source.ownerObject != null ? source.ownerObject.name : "Unknown";
                    Debug.Log($"[Character Collector] Source: {sourceName} - {languages.Count} languages, {terms.Count} terms");

                    // Iterate through all terms and languages
                    foreach (var term in terms)
                    {
                        for (int langIdx = 0; langIdx < languages.Count; langIdx++)
                        {
                            string langName = languages[langIdx].Name;

                            // Check language filter
                            if (hasScannedLanguages && showLanguageFilter)
                            {
                                if (!selectedLanguages.ContainsKey(langName) || !selectedLanguages[langName])
                                {
                                    continue; // Skip this language
                                }
                            }

                            string translation = term.Languages[langIdx];
                            if (string.IsNullOrEmpty(translation)) continue;

                            // Track per language
                            if (!languageStats.ContainsKey(langName))
                                languageStats[langName] = 0;

                            foreach (char c in translation)
                            {
                                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                                    continue;

                                allChars.Add(c);
                                totalCharacters++;
                                languageStats[langName]++;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Character Collector] Lỗi khi thu thập từ I2: {ex.Message}");
            }
        }

        private void CollectFromUnityLocalization(HashSet<char> allChars)
        {
            try
            {
                var collections = LocalizationEditorSettings.GetStringTableCollections();
                if (collections == null || collections.Count == 0)
                {
                    Debug.LogWarning("[Character Collector] Không tìm thấy String Table Collection nào (Unity Localization)!");
                    return;
                }

                int tablesProcessed = 0;
                foreach (var collection in collections)
                {
                    foreach (var table in collection.StringTables)
                    {
                        if (table == null) continue;

                        var code = table.LocaleIdentifier.Code;
                        var locale = LocalizationEditorSettings.GetLocale(code);
                        string langKey = locale != null ? locale.LocaleName : code;

                        if (hasScannedLanguages && showLanguageFilter && !IsLocaleSelected(langKey, code, locale))
                            continue;

                        if (!languageStats.ContainsKey(langKey)) languageStats[langKey] = 0;
                        tablesProcessed++;

                        foreach (var entry in table.Values)
                        {
                            if (entry == null || string.IsNullOrEmpty(entry.Value)) continue;
                            foreach (char c in entry.Value)
                            {
                                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t') continue;
                                allChars.Add(c);
                                totalCharacters++;
                                languageStats[langKey]++;
                            }
                        }
                    }
                }

                Debug.Log($"[Character Collector] Unity Localization: xử lý {tablesProcessed} bảng từ {languageStats.Count} ngôn ngữ.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Character Collector] Error: {ex.Message}");
            }
        }

        // Locale được chọn chưa: khớp thẳng theo LocaleName, không thì map qua tên I2 (EN/VI...).
        private bool IsLocaleSelected(string langKey, string code, Locale locale)
        {
            if (selectedLanguages.TryGetValue(langKey, out var v) && v) return true;

            string i2FromLocale = LocalizationManager.GetSupportedLanguage(locale != null ? locale.LocaleName : code);
            if (string.IsNullOrEmpty(i2FromLocale)) i2FromLocale = LocalizationManager.GetSupportedLanguage(code);
            if (string.IsNullOrEmpty(i2FromLocale)) return false;

            foreach (var kv in selectedLanguages)
            {
                if (!kv.Value) continue;
                var i2FromSel = LocalizationManager.GetSupportedLanguage(kv.Key);
                if (!string.IsNullOrEmpty(i2FromSel) &&
                    string.Equals(i2FromLocale, i2FromSel, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void AddCharacters(HashSet<char> allChars, string chars)
        {
            foreach (char c in chars)
            {
                allChars.Add(c);
            }
        }

        private void RemoveCharacters(HashSet<char> allChars, string chars)
        {
            foreach (char c in chars)
            {
                allChars.Remove(c);
            }
        }

        private const int AtlasSize = 2048;
        private const int MinPointSize = 8;
        private const int MaxPointSize = 256;

        // Nạp ký tự vào atlas qua API công khai TryAddCharacters. Cố định 2048; nếu bật auto-size thì nhị phân
        // tìm point size LỚN NHẤT còn nhét vừa 1 texture (giống Auto-Sizing của Font Asset Creator).
        private void UpdateFontAtlas()
        {
            if (targetFont == null)
            {
                EditorUtility.DisplayDialog("Error", "Vui lòng chọn Target Font trước!", "OK");
                return;
            }
            if (string.IsNullOrEmpty(collectedCharacters))
            {
                EditorUtility.DisplayDialog("Error", "Vui lòng thu thập ký tự trước!", "OK");
                return;
            }

            var sourceFont = GetSourceFont(targetFont);
            if (sourceFont == null)
            {
                EditorUtility.DisplayDialog("Error", "Không tìm được Source Font File (.ttf/.otf) của font asset này.", "OK");
                return;
            }

            int padding = targetFont.creationSettings.padding > 0 ? targetFont.creationSettings.padding : 9;
            var originalMode = targetFont.atlasPopulationMode;

            try
            {
                targetFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;

                // Baseline: ký tự font gốc vốn không có glyph (probe multi-atlas nên không thể tràn).
                var fontMissing = new HashSet<char>();
                var baseProbe = MakeProbe(sourceFont, MinPointSize, padding, true);
                if (baseProbe != null)
                {
                    baseProbe.TryAddCharacters(collectedCharacters, out string baseMissing);
                    if (!string.IsNullOrEmpty(baseMissing)) foreach (var c in baseMissing) fontMissing.Add(c);
                    Object.DestroyImmediate(baseProbe);
                }

                int pointSize = (int)targetFont.faceInfo.pointSize;
                var bestFace = targetFont.faceInfo;
                bool multiAtlas = true;

                if (autoSizePointSize)
                {
                    int lo = MinPointSize, hi = MaxPointSize, best = -1;
                    while (lo <= hi)
                    {
                        int mid = (lo + hi) / 2;
                        EditorUtility.DisplayProgressBar("Character Collector", $"Auto-size: thử point size {mid}…", 0.5f);

                        var probe = MakeProbe(sourceFont, mid, padding, false);
                        bool fits = false;
                        if (probe != null)
                        {
                            probe.TryAddCharacters(collectedCharacters, out string m);
                            fits = (probe.atlasTextures?.Length ?? 1) == 1 && OnlyFontMissing(m, fontMissing);
                            if (fits) bestFace = probe.faceInfo;
                            Object.DestroyImmediate(probe);
                        }

                        if (fits) { best = mid; lo = mid + 1; }
                        else hi = mid - 1;
                    }

                    if (best > 0) { pointSize = best; multiAtlas = false; }
                    else
                    {
                        // Nhỏ nhất vẫn không vừa 1 texture → chấp nhận nhiều texture ở size nhỏ nhất.
                        pointSize = MinPointSize;
                        var fallback = MakeProbe(sourceFont, MinPointSize, padding, true);
                        if (fallback != null) { bestFace = fallback.faceInfo; Object.DestroyImmediate(fallback); }
                    }
                }

                EditorUtility.DisplayProgressBar("Character Collector", $"Dựng atlas ở point size {pointSize}…", 0.9f);

                // Giữ nguyên TỈ LỆ metrics cũ, chỉ scale theo point size mới: chỉnh tay lineHeight/baseline
                // trước đó không bị ghi đè, layout text không nhảy dòng.
                var oldFace = targetFont.faceInfo;
                targetFont.faceInfo = PreserveMetrics(oldFace, bestFace);
                targetFont.isMultiAtlasTexturesEnabled = multiAtlas;
                SetAtlasSize(targetFont, AtlasSize);
                targetFont.ClearFontAssetData(true);
                targetFont.TryAddCharacters(collectedCharacters, out string missing);

                targetFont.atlasPopulationMode = originalMode;
                EditorUtility.SetDirty(targetFont);
                AssetDatabase.SaveAssets();

                int missingCount = string.IsNullOrEmpty(missing) ? 0 : missing.Length;
                int texCount = targetFont.atlasTextures?.Length ?? 1;
                if (missingCount > 0)
                    Debug.LogWarning($"[Character Collector] {missingCount} ký tự font gốc không có glyph: {missing}");
                Debug.Log($"[Character Collector] point size {pointSize} · {uniqueCharacters - missingCount} ký tự · {texCount} texture {AtlasSize}x{AtlasSize} · '{targetFont.name}'.");
                ShowNotification(new GUIContent(texCount > 1 ? $"✓ {texCount} textures @ {pointSize}" : $"✓ 1 texture @ {pointSize}"));
            }
            catch (System.Exception ex)
            {
                targetFont.atlasPopulationMode = originalMode;
                EditorUtility.DisplayDialog("Error", $"Lỗi: {ex.Message}", "OK");
                Debug.LogError($"[Character Collector] Error: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // Lấy metrics MỚI (đúng point size đã chọn) nhưng scale từ metrics CŨ theo tỉ lệ point size, để mọi
        // chỉnh tay trước đó được bảo toàn. superscript/subscriptSize là tỉ lệ, không scale.
        private static FaceInfo PreserveMetrics(FaceInfo oldFace, FaceInfo newFace)
        {
            if (oldFace.pointSize <= 0) return newFace;

            float k = newFace.pointSize / (float)oldFace.pointSize;
            var f = newFace;
            f.lineHeight = oldFace.lineHeight * k;
            f.ascentLine = oldFace.ascentLine * k;
            f.descentLine = oldFace.descentLine * k;
            f.baseline = oldFace.baseline * k;
            f.capLine = oldFace.capLine * k;
            f.meanLine = oldFace.meanLine * k;
            f.tabWidth = oldFace.tabWidth * k;
            f.underlineOffset = oldFace.underlineOffset * k;
            f.underlineThickness = oldFace.underlineThickness * k;
            f.strikethroughOffset = oldFace.strikethroughOffset * k;
            f.strikethroughThickness = oldFace.strikethroughThickness * k;
            f.superscriptOffset = oldFace.superscriptOffset * k;
            f.subscriptOffset = oldFace.subscriptOffset * k;
            return f;
        }

        // Font Static bị TMP null hoá sourceFontFile (khỏi kéo file font gốc vào build) — tham chiếu bền
        // còn lại là m_SourceFontFileGUID, chính chỗ Inspector dựng lại font gốc.
        private static Font GetSourceFont(TMP_FontAsset font)
        {
            if (font.sourceFontFile != null) return font.sourceFontFile;

            var guid = new SerializedObject(font).FindProperty("m_SourceFontFileGUID")?.stringValue;
            if (string.IsNullOrEmpty(guid)) return null;

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Font>(path);
        }

        // Font asset tạm để thử một point size — không đụng tới asset thật.
        private static TMP_FontAsset MakeProbe(Font sourceFont, int pointSize, int padding, bool multiAtlas)
        {
            return TMP_FontAsset.CreateFontAsset(sourceFont, pointSize, padding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, multiAtlas);
        }

        // Mọi ký tự thiếu đều thuộc loại font gốc không có glyph → coi như vừa (không phải tràn atlas).
        private static bool OnlyFontMissing(string missing, HashSet<char> fontMissing)
        {
            if (string.IsNullOrEmpty(missing)) return true;
            foreach (var c in missing)
                if (!fontMissing.Contains(c)) return false;
            return true;
        }

        // Đổi kích thước atlas: ghi serialized field + creation settings + reinit texture để render đúng size mới.
        private static void SetAtlasSize(TMP_FontAsset font, int size)
        {
            var so = new SerializedObject(font);
            so.FindProperty("m_AtlasWidth").intValue = size;
            so.FindProperty("m_AtlasHeight").intValue = size;
            var cs = so.FindProperty("m_CreationSettings");
            cs.FindPropertyRelative("atlasWidth").intValue = size;
            cs.FindPropertyRelative("atlasHeight").intValue = size;
            so.ApplyModifiedPropertiesWithoutUndo();

            var tex = font.atlasTexture;
            if (tex != null && (tex.width != size || tex.height != size))
            {
                tex.Reinitialize(size, size);
                tex.Apply(false);
            }
        }
    }
}

