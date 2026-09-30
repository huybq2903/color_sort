using UnityEngine;
using TMPro;
using I2.Loc;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Falcon.Modules.LocalizationService.Runtime
{
    [DisallowMultipleComponent]
    public class AutoI2TermBinder : MonoBehaviour
    {
        // ====== TERMS ======
        [Header("Terms (Runtime)")]
        [SerializeField] private string _term = string.Empty;             // Primary (Text)
        [SerializeField] private string _secondaryTerm = string.Empty;    // Secondary (Font/Font_TMP/...)

        // ====== PARAMS ======
        //[Header("Params (Optional)")]
        [Tooltip("Bật để áp các params Key/Value dưới đây vào LocalizationParamsManager.")]
        [SerializeField] private bool _enableParams = false;

        [Tooltip("Danh sách params áp sẵn (preview/default). Term phải dùng token I2 dạng {[key]} (vd: {[p0]}, {[player]}...).")]
        [SerializeField] private List<ParamKV> _paramBindings = new List<ParamKV>();

#if UNITY_EDITOR
        // ====== CREATE TERM (EDITOR ONLY) ======
        //[Header("Create Term (Editor Only)")]
        [Tooltip("LanguageSourceAsset đích khi tạo term (không ảnh hưởng dropdown chọn term).")]
        [SerializeField, HideInInspector] private LanguageSourceAsset _createTargetLSA;

        [Tooltip("Thử auto-translate qua I2 WebService (nếu có). Nếu thất bại sẽ copy giá trị base cho các ngôn ngữ khác.")]
        [SerializeField] private bool _autoTranslateOnCreate = true;

        [SerializeField] private bool _wasPushedToSource = false; // trạng thái lưu vết đã từng push
#endif

        // ====== RUNTIME CACHES ======
        private object[] _runtimeArgs = null;                       // positional (p0, p1, ...)
        private Dictionary<string, object> _runtimeNamed = null;    // named (key -> value)

        private void Reset()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(_secondaryTerm))
                _secondaryTerm = GetComponent<TMP_Text>() ? "Font_TMP" : "Font";
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(_secondaryTerm))
                _secondaryTerm = GetComponent<TMP_Text>() ? "Font_TMP" : "Font";
#endif
        }

        private void OnEnable()
        {
            EnsureLocalizeRuntime();
        }

        // ======================================================================
        // RUNTIME
        // ======================================================================
        /// <summary>Đảm bảo có Localize + set term; nếu có Params thì áp Params + OnLocalize(true).</summary>
        public void EnsureLocalizeRuntime()
        {
            if (string.IsNullOrEmpty(_term) && string.IsNullOrEmpty(_secondaryTerm))
                return;

            var loc = GetOrAddLocalize();
            if (_term == "") _term = "-";
            loc.SetTerm(_term, _secondaryTerm);

            bool hasInspectorParams = _enableParams && _paramBindings != null && _paramBindings.Count > 0;
            bool hasRuntimeParams = (_runtimeArgs != null && _runtimeArgs.Length > 0) ||
                                    (_runtimeNamed != null && _runtimeNamed.Count > 0);

            if (hasInspectorParams || hasRuntimeParams)
            {
                var lpm = GetOrAddParams();
                ApplyInspectorParams(lpm);
                ApplyRuntimeArgs(lpm);
                ApplyRuntimeNamed(lpm);
                loc.OnLocalize(true);
            }
        }

        // ======================================================================
        // PUBLIC API (I2Localize-like + Params)
        // ======================================================================

        /// <summary>Giống I2.Localize.SetTerm(primary, secondary).</summary>
        public void SetTerm(string primary, string secondary = null)
        {
            _term = primary ?? string.Empty;
            if (secondary != null) _secondaryTerm = secondary;
            var loc = GetOrAddLocalize();
            loc.SetTerm(_term, _secondaryTerm);
            MaybeRelocalizeIfParams();
        }

        public void SetPrimaryTerm(string primary) => SetTerm(primary, null);

        public void SetSecondaryTerm(string secondary)
        {
            _secondaryTerm = secondary ?? string.Empty;
            var loc = GetOrAddLocalize();
            loc.SetTerm(_term, _secondaryTerm);
            MaybeRelocalizeIfParams();
        }

        public void OnLocalize(bool force = true) => GetComponent<Localize>()?.OnLocalize(force);
        public Localize TryGetLocalize() => GetComponent<Localize>();
        public Localize GetOrAddLocalize() => GetComponent<Localize>() ?? gameObject.AddComponent<Localize>();
        public LocalizationParamsManager GetOrAddParams() => GetComponent<LocalizationParamsManager>() ?? gameObject.AddComponent<LocalizationParamsManager>();
        public string GetPrimaryTerm() => _term;
        public string GetSecondaryTerm() => _secondaryTerm;

        // ---------- Params API ----------
        /// <summary>Đặt positional args → {[p0]}, {[p1]}, ...</summary>
        public void SetArgs(params object[] args) { _runtimeArgs = args; }
        /// <summary>Đặt 1 param theo key → {[key]}</summary>
        public void SetParam(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _runtimeNamed ??= new Dictionary<string, object>();
            _runtimeNamed[key] = value;
            Refresh();
        }
        /// <summary>Đặt nhiều params (key->value).</summary>
        public void SetParams(Dictionary<string, object> dict)
        {
            if (dict == null || dict.Count == 0) return;
            _runtimeNamed ??= new Dictionary<string, object>();
            foreach (var kv in dict) _runtimeNamed[kv.Key] = kv.Value;
            Refresh();
        }
        /// <summary>Xoá cache params runtime.</summary>
        public void ClearParams()
        {
            _runtimeArgs = null;
            _runtimeNamed = null;
        }
        /// <summary>Áp (Inspector + Runtime Params) và gọi OnLocalize(true).</summary>
        public void Refresh()
        {
            var loc = GetOrAddLocalize();
            var lpm = GetOrAddParams();
            ApplyInspectorParams(lpm);
            ApplyRuntimeArgs(lpm);
            ApplyRuntimeNamed(lpm);
            loc.OnLocalize(true);
        }

        private void MaybeRelocalizeIfParams()
        {
            bool hasInspectorParams = _enableParams && _paramBindings != null && _paramBindings.Count > 0;
            bool hasRuntimeParams = (_runtimeArgs != null && _runtimeArgs.Length > 0) ||
                                    (_runtimeNamed != null && _runtimeNamed.Count > 0);
            if (hasInspectorParams || hasRuntimeParams) Refresh();
        }

        private static void ApplyInspectorParams(LocalizationParamsManager pm, List<ParamKV> bindings)
        {
            if (pm == null || bindings == null) return;
            foreach (var kv in bindings)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                pm.SetParameterValue(kv.Key, kv.Value ?? string.Empty);
            }
        }
        private void ApplyInspectorParams(LocalizationParamsManager pm)
        {
            if (!_enableParams) return;
            ApplyInspectorParams(pm, _paramBindings);
        }

        private static void ApplyRuntimeArgs(LocalizationParamsManager pm, object[] args)
        {
            if (pm == null || args == null || args.Length == 0) return;
            for (int i = 0; i < args.Length; i++)
                pm.SetParameterValue($"p{i}", args[i]?.ToString() ?? string.Empty);
        }
        private void ApplyRuntimeArgs(LocalizationParamsManager pm) => ApplyRuntimeArgs(pm, _runtimeArgs);

        private static void ApplyRuntimeNamed(LocalizationParamsManager pm, Dictionary<string, object> map)
        {
            if (pm == null || map == null || map.Count == 0) return;
            foreach (var kv in map)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                pm.SetParameterValue(kv.Key, kv.Value?.ToString() ?? string.Empty);
            }
        }
        private void ApplyRuntimeNamed(LocalizationParamsManager pm) => ApplyRuntimeNamed(pm, _runtimeNamed);

        // ====== DATA ======
        [System.Serializable]
        public struct ParamKV
        {
            public string Key;
            public string Value;
        }

#if UNITY_EDITOR
        // ======================================================================
        // CUSTOM INSPECTOR
        // ======================================================================
        [CustomEditor(typeof(AutoI2TermBinder)), CanEditMultipleObjects]
        public class AutoI2TermBinderEditor : Editor
        {
            // Serialized
            SerializedProperty _pTerm, _pSecTerm;
            SerializedProperty _pEnableParams, _pParamBindings;
            SerializedProperty _pCreateTargetLSA, _pAutoTranslate, _pWasPushed;

            // UI foldouts
            static bool _showCreateSection = false;

            // Inputs cho Create
            string _explicitTerm = string.Empty;
            string _prefix = string.Empty;

            void OnEnable()
            {
                _pTerm = serializedObject.FindProperty("_term");
                _pSecTerm = serializedObject.FindProperty("_secondaryTerm");
                _pEnableParams = serializedObject.FindProperty("_enableParams");
                _pParamBindings = serializedObject.FindProperty("_paramBindings");

                _pCreateTargetLSA = serializedObject.FindProperty("_createTargetLSA");
                _pAutoTranslate = serializedObject.FindProperty("_autoTranslateOnCreate");
                _pWasPushed = serializedObject.FindProperty("_wasPushedToSource");

                for (int i = 0; i < targets.Length; i++)
                {
                    var binder = targets[i] as AutoI2TermBinder;
                    if (!binder) continue;

                    LanguageSourceAsset lsaUsed = DetectLSA(binder);
                }
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                // ===== CHỌN TERM & PREVIEW =====
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Chọn Term & Preview", EditorStyles.boldLabel);

                var allTerms = GetAllTermsFromResources();
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    if (allTerms.Length == 0)
                    {
                        EditorGUILayout.HelpBox("Không tìm thấy LanguageSourceAsset trong Resources/ hoặc chưa có term.", MessageType.Warning);
                    }
                    else
                    {
                        DrawTwoColumnTermPickers(allTerms, _pTerm, _pSecTerm);
                    }

                    if (GUILayout.Button("Preview (Add Localize & Apply)"))
                    {
                        foreach (var t in targets)
                        {
                            var binder = t as AutoI2TermBinder;
                            binder?.EnsureLocalizeRuntime();
                            EditorUtility.SetDirty(binder);
                        }
                    }
                }

                // ===== PARAMS (ngay dưới phần chọn term) =====
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Params (Optional)", EditorStyles.boldLabel);
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.PropertyField(_pEnableParams, new GUIContent("Enable"));
                    if (_pEnableParams.boolValue)
                    {
                        EditorGUILayout.PropertyField(_pParamBindings, new GUIContent("Bindings"), true);
                        EditorGUILayout.HelpBox("Dùng token I2 dạng {[key]} trong term. Positional args sẽ map p0, p1, ... qua SetArgs(...).", MessageType.Info);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Preview Params Apply"))
                            {
                                foreach (var t in targets)
                                {
                                    var binder = t as AutoI2TermBinder;
                                    if (!binder) continue;
                                    binder.EnsureLocalizeRuntime();
                                    EditorUtility.SetDirty(binder);
                                }
                            }
                            /*if (GUILayout.Button("Clear Runtime Params", GUILayout.Width(180)))
                            {
                                foreach (var t in targets)
                                {
                                    var binder = t as AutoI2TermBinder;
                                    if (!binder) continue;
                                    binder.ClearParams();
                                    EditorUtility.SetDirty(binder);
                                }
                            }*/
                        }
                    }
                }

                // ===== TẠO TERM (để CUỐI CÙNG) =====
                EditorGUILayout.Space(8);
                _showCreateSection = EditorGUILayout.Foldout(_showCreateSection, "Tạo Term (Editor Only)", true);
                if (_showCreateSection)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        // --- LSA (Session Only): nếu để trống, mỗi binder sẽ tự detect riêng ---
                        LanguageSourceAsset sessionLSA = _pCreateTargetLSA.objectReferenceValue as LanguageSourceAsset;
                        sessionLSA = (LanguageSourceAsset)EditorGUILayout.ObjectField(
                            "Language Source (Session Only)", sessionLSA, typeof(LanguageSourceAsset), false);

                        // Auto-translate toggle (giữ nguyên)
                        EditorGUILayout.PropertyField(_pAutoTranslate, new GUIContent("Auto-translate"));

                        EditorGUILayout.Space(6);
                        // Giữ nguyên nội dung helpbox như bạn đang dùng
                        EditorGUILayout.HelpBox(
                            "Bạn có thể nhập 'Tiền tố' hoặc 'Term đầy đủ'.\n" +
                            "- Nếu nhập Term đầy đủ: tất cả TMP sẽ dùng term này.\n" +
                            "- Nếu nhập Tiền tố: tất cả TMP sẽ build key theo tiền tố đó.\n" +
                            "- Nếu để trống cả hai: mỗi TMP sẽ tự detect prefix theo LSA gần nhất của chính nó (hoặc dùng 'UI').\n" +
                            "- Tôi sẽ coi text gốc của bạn là tiếng Anh và dịch sang các ngôn ngữ khác. Nếu dịch thử và thấy 3 ngôn ngữ zh-CN, zh-TW, es không thay đổi gì, tôi sẽ không tạo term"
                            ,
                            MessageType.Info);

                        // Các field Inspector giữ nguyên
                        _prefix = EditorGUILayout.TextField(new GUIContent("Nhập tiền tố, tự sinh hậu tố"), _prefix);
                        _explicitTerm = EditorGUILayout.TextField(new GUIContent("Nhập term đầy đủ"), _explicitTerm);

                        // ==== PREVIEW + CHUẨN BỊ DỮ LIỆU TẠO TERM (một vòng lặp duy nhất) ====
                        var candidateMap = new System.Collections.Generic.Dictionary<AutoI2TermBinder, string>();
                        string preview = null;
                        bool mixed = false;

                        for (int i = 0; i < targets.Length; i++)
                        {
                            var binder = targets[i] as AutoI2TermBinder;
                            if (!binder) continue;

                            LanguageSourceAsset lsaUsed = DetectLSA(binder, sessionLSA);
                            string english = GetLabelEnglish(binder);
                            string candidate = CreateCandidate(english, lsaUsed, _explicitTerm, _prefix);

                            // Lưu lại để dùng cho preview & tạo term
                            candidateMap[binder] = candidate;

                            // Tính preview mixed-value theo chuẩn Unity
                            if (preview == null) preview = candidate;
                            else if (preview != candidate) mixed = true;
                        }

                        // Vẽ preview (multi-select aware)
                        EditorGUI.showMixedValue = mixed;
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.TextField("Preview", preview ?? "");
                        }
                        EditorGUI.showMixedValue = false;

                        // ==== BUTTON TẠO TERM: dùng lại candidateMap, không build lại ====
                        if (GUILayout.Button("Tạo term"))
                        {
                            serializedObject.ApplyModifiedProperties();

                            foreach (var kv in candidateMap)
                            {
                                // kv.Key  : binder
                                // kv.Value: candidate đã tính ở trên
                                TryCreateAndPushTerm_ForBinder(kv.Key, kv.Value, false);
                            }

                            serializedObject.Update();
                        }

                        if (_pWasPushed != null && _pWasPushed.boolValue)
                            EditorGUILayout.HelpBox("Đã từng push ít nhất 1 term vào LSA từ component này.", MessageType.Info);
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }

            // ===== API =====

            public static void TryCreateAndPushTerm_API(AutoI2TermBinder binder, TMP_Text tmp)
            {
                LanguageSourceAsset lsaUsed = DetectLSA(binder);
                string english = GetLabelEnglish(binder);
                string candidate = CreateCandidate(english, lsaUsed);
                TryCreateAndPushTerm_ForBinder(binder, candidate, false);
            }

            // ===== Helpers UI =====

            // === Helpers chung cho TMP_Text & Text (Editor-only) ===
            private static string GetLabelEnglish(AutoI2TermBinder binder)
            {
                var tmp = binder.GetComponent<TMPro.TMP_Text>();
                if (tmp) return tmp.text;
                var ui = binder.GetComponent<UnityEngine.UI.Text>();
                if (ui) return ui.text;
                return "";
            }

            private static LanguageSourceAsset DetectLSA(AutoI2TermBinder binder, LanguageSourceAsset sessionLSA = null)
            {
                // 0) Chọn LSA cho binder này (luôn chạy)
                var lsaUsed = sessionLSA;
                if (lsaUsed == null)
                {
                    var path = GetAssetOrPrefabPath(binder);
                    if (!string.IsNullOrEmpty(path))
                        lsaUsed = FindClosestLsaForPath(path);
                }

                var soBinder = new SerializedObject(binder);
                var spLSA = soBinder.FindProperty("_createTargetLSA");
                if (spLSA != null && spLSA.objectReferenceValue != lsaUsed)
                {
                    spLSA.objectReferenceValue = lsaUsed;
                    soBinder.ApplyModifiedPropertiesWithoutUndo();
                }
                return lsaUsed;
            }

            private static string CreateCandidate(string stringInsideTmpOrTextUI, LanguageSourceAsset lsaUsed, string _explicitTerm = "", string _prefix = "")
            {
                string baseEn = stringInsideTmpOrTextUI ?? "";
                if (string.IsNullOrWhiteSpace(baseEn)) return "";

                int maxChars = 24;

                // 1) Quyết định candidate theo ưu tiên
                string candidate;
                if (!string.IsNullOrWhiteSpace(_explicitTerm))
                {
                    // 1) Ưu tiên term đầy đủ do user nhập
                    candidate = _explicitTerm.Trim();
                }
                else if (!string.IsNullOrWhiteSpace(_prefix))
                {
                    // 2) Nếu có tiền tố người dùng nhập → build theo tiền tố này
                    candidate = BuildTermFromEnglishPrefix(_prefix.Trim(), baseEn, maxChars);
                }
                else
                {
                    string px = DerivePrefixFromLsaPath(lsaUsed);
                    if (string.IsNullOrEmpty(px)) px = "UI";
                    candidate = BuildTermFromEnglishPrefix(px, baseEn, maxChars);
                }
                return candidate;
            }

            private static void DrawTwoColumnTermPickers(string[] allTerms, SerializedProperty primaryProp, SerializedProperty secondaryProp)
            {
                EditorGUILayout.BeginHorizontal();

                // LEFT: Primary
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Primary", EditorStyles.miniBoldLabel);
                DrawTermPopupImmediate(primaryProp, allTerms);
                EditorGUILayout.EndVertical();

                // RIGHT: Secondary
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Secondary", EditorStyles.miniBoldLabel);
                DrawTermPopupImmediate(secondaryProp, allTerms);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
            }

            private static void DrawTermPopupImmediate(SerializedProperty prop, string[] allTerms)
            {
                var display = new string[allTerms.Length + 1];
                display[0] = "— None —";
                for (int i = 0; i < allTerms.Length; i++)
                    display[i + 1] = allTerms[i];

                EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;

                int selected = 0; // none
                if (!prop.hasMultipleDifferentValues && !string.IsNullOrEmpty(prop.stringValue))
                {
                    int found = System.Array.IndexOf(allTerms, prop.stringValue);
                    selected = (found >= 0) ? found + 1 : 0;
                }

                int newSel = EditorGUILayout.Popup(selected, display);
                if (newSel != selected)
                {
                    prop.serializedObject.Update();
                    prop.stringValue = (newSel == 0) ? string.Empty : allTerms[newSel - 1];
                    prop.serializedObject.ApplyModifiedProperties();
                }

                EditorGUI.showMixedValue = false;
            }

            private static string BuildTermFromEnglishPrefix(string prefix, string english, int maxChars)
            {
                if (string.IsNullOrWhiteSpace(prefix)) prefix = "UI";
                if (!prefix.EndsWith("/")) prefix += "/";

                var body = english ?? string.Empty;
                body = Regex.Replace(body, "<.*?>", " ");                 // bỏ richText tags
                body = body.Replace('\n', ' ').Replace('\r', ' ');
                body = Regex.Replace(body, "\\s+", " ").Trim();
                if (string.IsNullOrEmpty(body)) return "-";

                if (body.Length > maxChars)
                {
                    int cut = body.LastIndexOf(' ', Mathf.Clamp(maxChars, 1, body.Length - 1));
                    if (cut <= 0) cut = maxChars;
                    body = body.Substring(0, cut);
                }

                body = I2Utils.GetValidTermName(body).Replace(' ', '_');
                return prefix + body;
            }

            // ===== Collect all terms from LSA in Resources =====
            private static string[] GetAllTermsFromResources()
            {
                var termSet = new HashSet<string>(System.StringComparer.Ordinal);
                var guids = AssetDatabase.FindAssets("t:LanguageSourceAsset");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path)) continue;
                    if (!path.Contains("/Resources/")) continue; // chỉ load từ Resources

                    var lsa = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(path);
                    if (lsa == null || lsa.mSource == null || lsa.mSource.mTerms == null) continue;

                    foreach (var td in lsa.mSource.mTerms)
                    {
                        if (td == null || string.IsNullOrEmpty(td.Term)) continue;
                        termSet.Add(td.Term);
                    }
                }

                var arr = termSet.ToArray();
                System.Array.Sort(arr, System.StringComparer.Ordinal);
                return arr;
            }

            // ===== Detect Helpers: prefab path, closest LSA, derive prefix =====

            // Lấy đường dẫn prefab/asset (kể cả khi đang chọn instance trong scene / prefab mode)
            private static string GetAssetOrPrefabPath(Object obj)
            {
                if (obj == null) return string.Empty;

#if UNITY_2018_3_OR_NEWER
                // Nếu đang ở Prefab Mode: lấy assetPath của prefab đang edit
                var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null && !string.IsNullOrEmpty(stage.assetPath))
                    return stage.assetPath;
#endif

                GameObject go = null;
                if (obj is AutoI2TermBinder binder && binder != null) go = binder.gameObject;
                else if (obj is Component c && c != null) go = c.gameObject;
                else if (obj is GameObject g) go = g;

                if (go != null)
                {
#if UNITY_2018_3_OR_NEWER
                    // Instance trong scene → path prefab gốc
                    var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    if (!string.IsNullOrEmpty(prefabPath))
                        return prefabPath;
#endif
                    var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (src != null)
                    {
                        var p = AssetDatabase.GetAssetPath(src);
                        if (!string.IsNullOrEmpty(p)) return p;
                    }
                    if (PrefabUtility.IsPartOfPrefabAsset(go))
                    {
                        var p = AssetDatabase.GetAssetPath(go);
                        if (!string.IsNullOrEmpty(p)) return p;
                    }
#if UNITY_2018_3_OR_NEWER
                    var root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                    if (root != null)
                    {
                        var src2 = PrefabUtility.GetCorrespondingObjectFromSource(root);
                        var p2 = AssetDatabase.GetAssetPath(src2);
                        if (!string.IsNullOrEmpty(p2)) return p2;
                    }
#endif
                }

                var generic = AssetDatabase.GetAssetPath(obj);
                return string.IsNullOrEmpty(generic) ? string.Empty : generic;
            }

            private static string GetFolderOfPath(string assetPath)
            {
                if (string.IsNullOrEmpty(assetPath)) return string.Empty;
                int slash = assetPath.LastIndexOf('/');
                return (slash <= 0) ? string.Empty : assetPath.Substring(0, slash);
            }
            private static string GetParentFolder(string folderPath)
            {
                if (string.IsNullOrEmpty(folderPath)) return string.Empty;
                int slash = folderPath.LastIndexOf('/');
                return (slash <= 0) ? string.Empty : folderPath.Substring(0, slash);
            }

            // Khoảng cách 2 thư mục (để chọn LSA "gần" prefab hơn)
            private static int PathDistance(string dirA, string dirB)
            {
                if (string.IsNullOrEmpty(dirA) || string.IsNullOrEmpty(dirB)) return int.MaxValue;
                var A = dirA.Split('/');
                var B = dirB.Split('/');
                int lcp = 0, lim = Mathf.Min(A.Length, B.Length);
                while (lcp < lim && A[lcp] == B[lcp]) lcp++;
                return (A.Length - lcp) + (B.Length - lcp);
            }

            private static LanguageSourceAsset FindClosestLsaForPath(string assetPath)
            {
                string prefabDir = GetFolderOfPath(assetPath);
                string cursor = prefabDir;

                while (!string.IsNullOrEmpty(cursor))
                {
                    // Chỉ kiểm tra "Resources" ngay dưới cấp hiện tại
                    string resDir = cursor.EndsWith("/") ? cursor + "Resources" : cursor + "/Resources";
                    if (AssetDatabase.IsValidFolder(resDir))
                    {
                        var guids = AssetDatabase.FindAssets("t:LanguageSourceAsset", new[] { resDir });

                        LanguageSourceAsset best = null;
                        int bestDist = int.MaxValue;
                        int bestPathLen = int.MaxValue;

                        foreach (var g in guids)
                        {
                            var ap = AssetDatabase.GUIDToAssetPath(g);
                            if (string.IsNullOrEmpty(ap)) continue;

                            var lsa = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(ap);
                            if (lsa == null || lsa.mSource == null) continue;

                            var lsaDir = GetFolderOfPath(ap);
                            int dist = PathDistance(prefabDir, lsaDir);
                            int pathLen = lsaDir?.Length ?? int.MaxValue;

                            if (dist < bestDist || (dist == bestDist && pathLen < bestPathLen))
                            {
                                best = lsa; bestDist = dist; bestPathLen = pathLen;
                            }
                        }
                        if (best != null) return best; // trả về ngay cấp hiện tại nếu tìm thấy
                    }

                    // Lùi lên 1 cấp
                    string next = GetParentFolder(cursor);
                    if (next == cursor) break;
                    cursor = next;
                }

                return null;
            }


            // Suy prefix từ đường dẫn LSA (ưu tiên /Modules/{X}/.../Resources/, fallback folder trước /Resources/)
            private static string DerivePrefixFromLsaPath(LanguageSourceAsset lsa)
            {
                if (lsa == null) return string.Empty;
                var lsaPath = AssetDatabase.GetAssetPath(lsa);
                if (string.IsNullOrEmpty(lsaPath)) return string.Empty;

                Match m;
                //var m = System.Text.RegularExpressions.Regex.Match(
                //    lsaPath, @"/Modules/([^/]+)/.*?/Resources/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                //if (m.Success) return m.Groups[1].Value;

                m = System.Text.RegularExpressions.Regex.Match(
                    lsaPath, @"/Modules/([^/]+)/Resources/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value;

                m = System.Text.RegularExpressions.Regex.Match(
                    lsaPath, @"/([^/]+)/Resources/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value;

                return string.Empty;
            }



            // ===== CREATE / PUSH TERM vào LSA (nếu term tồn tại, phần English giống thì tự gắn vào) =====
            public static bool TryCreateAndPushTerm_ForBinder(AutoI2TermBinder binder, string term, bool recreateIfExists)
            {
                if (binder == null) return false;

                var so = new SerializedObject(binder);
                so.Update();
                var pCreateLSA = so.FindProperty("_createTargetLSA");
                var pAutoTrans = so.FindProperty("_autoTranslateOnCreate");
                var pWasPushed = so.FindProperty("_wasPushedToSource");
                var pTerm = so.FindProperty("_term");

                var lsa = pCreateLSA != null ? pCreateLSA.objectReferenceValue as LanguageSourceAsset : null;
                if (lsa == null || lsa.mSource == null)
                {
                    ShowTempNotification("Chưa kéo-thả LanguageSourceAsset (Target) vào component.");
                    return false;
                }
                if (string.IsNullOrEmpty(term))
                {
                    ShowTempNotification("Term rỗng.");
                    return false;
                }

                var src = lsa.mSource;
                var tdExisting = src.GetTermData(term);

                var tmp = binder.GetComponent<TMPro.TMP_Text>();
                var ui = binder.GetComponent<UnityEngine.UI.Text>();
                string baseTextRaw = tmp ? tmp.text : (ui ? ui.text : string.Empty);

                // === LIGHTWEIGHT CHECK (zh-CN, zh-TW, es) ===
                // Nếu dịch xong mà so với 3 ngôn ngữ trên không thấy khác biệt => không tạo term
                {
                    string Normalize(string s)
                        => string.IsNullOrEmpty(s) ? string.Empty
                           : System.Text.RegularExpressions.Regex
                               .Replace(System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", " "), "\\s+", " ")
                               .Trim().ToLowerInvariant();

                    var baseNorm = Normalize(baseTextRaw);
                    if (!string.IsNullOrEmpty(baseNorm))
                    {
                        // Dùng pipeline I2: AddQuery (tự protect) + ForceTranslate (tự restore)
                        var dict = new Dictionary<string, TranslationQuery>(System.StringComparer.Ordinal);

                        // base giả định tiếng Anh; nếu khác, đổi "en" ở đây
                        GoogleTranslation.AddQuery(baseTextRaw, "en", "zh-CN", dict);
                        GoogleTranslation.AddQuery(baseTextRaw, "en", "zh-TW", dict);
                        GoogleTranslation.AddQuery(baseTextRaw, "en", "es", dict);

                        bool ok = GoogleTranslation.ForceTranslate(dict, true);
                        if (ok && dict.TryGetValue(baseTextRaw, out var q2) && q2.Results != null && q2.Results.Length > 0)
                        {
                            bool allSame = true;
                            for (int i = 0; i < q2.Results.Length; i++)
                                allSame &= Normalize(q2.Results[i]) == baseNorm;

                            if (allSame)
                            {
                                ShowTempNotification("Bỏ qua tạo term: en == zh == es (không khác biệt).");
                                return false; // early-exit
                            }
                        }
                    }
                }

                int baseIdx = FindLanguageIndexByInternationalCode(src, "en");
                if (baseIdx < 0) baseIdx = src.GetLanguageIndex("English");
                if (baseIdx < 0 && src.mLanguages.Count > 0) baseIdx = 0;

                if (tdExisting != null && !recreateIfExists)
                {
                    string existingEN = tdExisting.GetTranslation(baseIdx) ?? string.Empty;
                    bool englishMatches = string.Equals(baseTextRaw, existingEN, System.StringComparison.Ordinal);

                    if (englishMatches)
                    {
                        Debug.LogError($"[AutoI2TermBinder] Term đã tồn tại, tự động chọn: {term} (GO: {binder.gameObject.name})");
                        if (pTerm != null) pTerm.stringValue = term;
                        if (pWasPushed != null) pWasPushed.boolValue = true;

                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(binder);
#if UNITY_2018_3_OR_NEWER
                        PrefabUtility.RecordPrefabInstancePropertyModifications(binder);
#endif
                        if (!Application.isPlaying && binder.gameObject.scene.IsValid())
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(binder.gameObject.scene);
                        EditorUtility.SetDirty(lsa);
                        AssetDatabase.SaveAssets();
                        ShowTempNotification($"Đã chọn term '{term}' trong {lsa.name}");
                        return true;
                    }
                    else
                    {
                        // ❗ khác English → tạo key mới bằng cách thêm _1, _2...
                        string candidate = term;
                        int suffix = 1;
                        while (src.GetTermData(candidate) != null)
                        {
                            candidate = term + "_" + suffix++;
                        }
                        term = candidate;

                        if (pTerm != null) pTerm.stringValue = term;
                        so.ApplyModifiedProperties();

                        tdExisting = null; // coi như chưa có → AddTerm bên dưới
                    }
                }

                // --- AUTO-DETECT PARAM TOKENS {[key]} (chỉ chạy nếu KHÔNG early-return ở trên) ---
                {
                    // Chỉ TÍNH toán trước, và CHỈ ghi vào SerializedObject nếu sắp tạo/chọn term
                    var keys = new System.Collections.Generic.HashSet<string>();
                    if (!string.IsNullOrEmpty(baseTextRaw))
                    {
                        // bỏ richtext, gom khoảng trắng
                        var norm = System.Text.RegularExpressions.Regex.Replace(baseTextRaw, "<.*?>", " ");
                        foreach (System.Text.RegularExpressions.Match m in
                                 System.Text.RegularExpressions.Regex.Matches(norm, @"\{\[\s*([A-Za-z0-9_]+)\s*\]\}"))
                        {
                            var k = m.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(k)) keys.Add(k);
                        }
                    }

                    if (keys.Count > 0)
                    {
                        var pEnable = so.FindProperty("_enableParams");
                        var pBinds = so.FindProperty("_paramBindings");
                        if (pEnable != null && pBinds != null)
                        {
                            pEnable.boolValue = true;
                            pBinds.arraySize = keys.Count;

                            int i = 0;
                            foreach (var k in keys)
                            {
                                var elem = pBinds.GetArrayElementAtIndex(i++);
                                elem.FindPropertyRelative("Key").stringValue = k;
                                elem.FindPropertyRelative("Value").stringValue = string.Empty; // default/preview
                            }
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                }
                // --- END AUTO-DETECT PARAMS ---

                // ===== TẠO/SYNC TERM MỚI =====
                var td = tdExisting ?? src.AddTerm(term, eTermType.Text);

                if (baseIdx >= 0 && string.IsNullOrEmpty(td.GetTranslation(baseIdx)))
                    td.SetTranslation(baseIdx, baseTextRaw);

                bool autoTranslate = pAutoTrans != null ? pAutoTrans.boolValue : true;
                if (autoTranslate)
                {
                    bool ok = TryAutoTranslateViaI2WebService(src, td, baseTextRaw, baseIdx);
                    if (!ok)
                    {
                        for (int li = 0; li < src.mLanguages.Count; li++)
                        {
                            if (li == baseIdx) continue;
                            if (!string.IsNullOrEmpty(td.GetTranslation(li))) continue;
                            td.SetTranslation(li, baseTextRaw);
                        }
                    }
                }

                if (pTerm != null) pTerm.stringValue = term;
                if (pWasPushed != null) pWasPushed.boolValue = true;

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(binder);
#if UNITY_2018_3_OR_NEWER
                PrefabUtility.RecordPrefabInstancePropertyModifications(binder);
#endif
                if (!Application.isPlaying && binder.gameObject.scene.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(binder.gameObject.scene);
                EditorUtility.SetDirty(lsa);
                AssetDatabase.SaveAssets();

                ShowTempNotification($"Đã tạo/sync '{term}' vào {lsa.name}");
                return true;
            }

            // ---- Auto-translate qua I2 WebService (nếu có) ----
            private static bool TryAutoTranslateViaI2WebService(LanguageSourceData src, TermData td, string baseText, int baseIdx)
            {
                try
                {
                    var fromCode = GoogleLanguages.GetLanguageCode(src.mLanguages[baseIdx].Name);
                    if (string.IsNullOrEmpty(fromCode)) fromCode = "en";

                    // Thu thập các mã đích chưa có bản dịch
                    var toCodes = new List<string>();
                    for (int i = 0; i < src.mLanguages.Count; i++)
                    {
                        if (i == baseIdx) continue;
                        if (!string.IsNullOrEmpty(td.GetTranslation(i))) continue;

                        var code = GoogleLanguages.GetLanguageCode(src.mLanguages[i].Name);
                        if (!string.IsNullOrEmpty(code)) toCodes.Add(code);
                    }
                    if (toCodes.Count == 0) return true;

                    // 1) Tạo TranslationDictionary theo đúng pipeline I2 (mỗi ngôn ngữ đích = 1 AddQuery)
                    var requests = new Dictionary<string, TranslationQuery>(System.StringComparer.Ordinal);
                    foreach (var to in toCodes)
                        GoogleTranslation.AddQuery(baseText, fromCode, to, requests); // tự protect param/tags

                    // 2) Dịch đồng bộ qua ForceTranslate(dict) (I2 sẽ parse & restore param)
                    var ok = GoogleTranslation.ForceTranslate(requests, true); // dùng webservice chuẩn của I2
                    if (!ok) return false;   // có thể log cảnh báo nếu muốn

                    // 3) Lấy kết quả và ghi vào TermData
                    var q = requests[baseText];                // ParseTranslationResult đã đổ vào đây
                    var results = q.Results;                   // mảng kết quả theo đúng TargetLanguagesCode
                    var targets = q.TargetLanguagesCode;

                    bool any = false;
                    if (results != null && targets != null)
                    {
                        for (int k = 0; k < results.Length && k < targets.Length; k++)
                        {
                            // map mã ngôn ngữ đích -> index trong source
                            int li = -1;
                            for (int j = 0; j < src.mLanguages.Count; j++)
                            {
                                var cj = GoogleLanguages.GetLanguageCode(src.mLanguages[j].Name);
                                if (string.Equals(cj, targets[k], System.StringComparison.OrdinalIgnoreCase))
                                { li = j; break; }
                            }
                            if (li < 0 || li == baseIdx) continue;
                            if (!string.IsNullOrEmpty(td.GetTranslation(li))) continue;

                            td.SetTranslation(li, results[k]); // token đã được I2 giữ nguyên
                            any = true;
                        }
                    }
                    return any;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[AutoI2TermBinder] Auto-translate exception: " + ex.Message);
                    return false;
                }
            }

            private static int FindLanguageIndexByInternationalCode(LanguageSourceData src, string intlCode)
            {
                for (int i = 0; i < src.mLanguages.Count; i++)
                {
                    var intl = GoogleLanguages.GetLanguageCode(src.mLanguages[i].Name);
                    if (string.Equals(intl, intlCode, System.StringComparison.OrdinalIgnoreCase))
                        return i;
                }
                return -1;
            }

            private static void ShowTempNotification(string message)
            {
#if UNITY_2021_1_OR_NEWER
                SceneView.lastActiveSceneView?.ShowNotification(new GUIContent(message));
#else
                EditorUtility.DisplayDialog("AutoI2", message, "OK");
#endif
            }
        }
#endif // UNITY_EDITOR
    }
}