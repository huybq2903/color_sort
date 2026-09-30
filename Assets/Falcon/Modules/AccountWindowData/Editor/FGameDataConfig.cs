/*
 * Author: Vo Hong Sang
 * Email: sangvh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-16
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Newtonsoft.Json;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Core.AccountData.Editor
{
    /// <summary>
    /// Editor window for managing FGameData<> instances.
    /// </summary>

    public class FGameDataConfig : EditorWindow
    {
        private static readonly JsonSerializerSettings s_jsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };
        private static readonly string[] s_emptyFgdTypeNames = { "(no FGameData<> found)" };
        private static List<Type> s_cachedFgdTypes;
        private static string[] s_cachedFgdTypeNames;

        [MenuItem("Falcon/Modules/Account/Window Data")]
        public static void Get()
        {
            var window = GetWindow<FGameDataConfig>("Account Window Data");
            window.Show();
        }

        private static readonly string[] TAB_TITLES = { "FGameData" }; // Can add more tabs here
        private const string PREFS_FGD_TYPE = "EventDataConfig_SelectedFGameDataType";
        private int selectedTab;
        private Vector2 scrollPosition;
        private string statusMessage = "";
        private double statusExpireTime;
        private UnityEditor.Editor editor;
        private PropertyTree fgdTree;
        private List<Type> fgdTypes;
        private string[] fgdTypeNames;
        private Vector2 fgdScroll;
        private int selectedFgdIndex = -1;
        private object currentFgdInstance;
        private string jsonBuffer = "";
        private const string key_game_datas = "falcon_game_datas_";

        private void OnEnable()
        {
            BuildFgdTypeList();
            RestoreSelectedFgdType();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // Vào/ra Play là đổi nguồn data nên phải bind lại, không thì đang giữ instance của domain cũ.
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.EnteredPlayMode or PlayModeStateChange.EnteredEditMode
                && selectedFgdIndex >= 0)
                LoadOrCreateInstanceForSelected();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (fgdTree != null)
            {
                fgdTree.Dispose();
                fgdTree = null;
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                selectedTab = GUILayout.Toolbar(
                    selectedTab,
                    TAB_TITLES,
                    EditorStyles.toolbarButton,
                    GUILayout.Height(22)
                );
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space();

            switch (selectedTab)
            {
                case 0:
                    DrawFGameDataTab();
                    break;
                    // TODO: Can add more tabs here
                    // Add Draw functions for new tabs
            }
        }

        private void BuildFgdTypeList()
        {
            if (s_cachedFgdTypes != null && s_cachedFgdTypeNames != null)
            {
                fgdTypes = s_cachedFgdTypes;
                fgdTypeNames = s_cachedFgdTypeNames;
                return;
            }

            var all = TypeCache.GetTypesDerivedFrom<FGameData>();
            s_cachedFgdTypes = all
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    IsSubclassOfOpenGeneric(t, typeof(FGameData<>)))
                .OrderBy(t => t.FullName)
                .ToList();

            s_cachedFgdTypeNames = s_cachedFgdTypes.Select(t => t.Name).ToArray();
            fgdTypes = s_cachedFgdTypes;
            fgdTypeNames = s_cachedFgdTypeNames;
        }

        private void RestoreSelectedFgdType()
        {
            var saved = EditorPrefs.GetString(PREFS_FGD_TYPE, "");
            selectedFgdIndex = Mathf.Clamp(
                string.IsNullOrEmpty(saved) ? -1 : fgdTypes.FindIndex(t => t.AssemblyQualifiedName == saved),
                -1, (fgdTypes?.Count ?? 0) - 1);

            if (selectedFgdIndex >= 0)
                LoadOrCreateInstanceForSelected();
        }

        private static bool IsSubclassOfOpenGeneric(Type toCheck, Type openGeneric)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (cur == openGeneric) return true;
                toCheck = toCheck.BaseType;
            }

            return false;
        }

        private static string GetFgdKey(Type t)
        {
            t.TryGetFgdTypeName(out var typeName);
            return key_game_datas + typeName;
        }

        private void LoadOrCreateInstanceForSelected()
        {
            var t = fgdTypes[selectedFgdIndex];
            var def = Activator.CreateInstance(t);
            object loaded = null;

            // Đang Play thì bám thẳng instance runtime, sửa/Force ăn ngay và không bị runtime save đè.
            if (EditorApplication.isPlaying)
            {
                loaded = AccountManager.Instance.GetGameData(t);
                jsonBuffer = "";
                BindTree(loaded ?? def);
                return;
            }

            try
            {
                var keyFgd = GetFgdKey(t);
                if (SaveLoadHandler.ExistsKey(keyFgd))
                {
                    jsonBuffer = SaveLoadHandler.Load<string>(keyFgd);
                    if (string.IsNullOrEmpty(jsonBuffer))
                    {
                        loaded = def;
                    }
                    else
                    {
                        var fGameData = (FGameData)JsonConvert.DeserializeObject(jsonBuffer, t, s_jsonSettings);
                        loaded = fGameData;
                    }
                }
                else
                {
                    loaded = def;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SaveGamePro load failed for {t.FullName}: {e.Message}");
            }

            BindTree(loaded ?? def);
        }

        private void BindTree(object instance)
        {
            currentFgdInstance = instance;
            if (fgdTree != null)
            {
                fgdTree.Dispose();
                fgdTree = null;
            }
            fgdTree = currentFgdInstance != null
                ? PropertyTree.Create(currentFgdInstance, SerializationBackend.Odin)
                : null;
            ExpandAllFgdTree();
        }

        private void SaveSelectedFgdType()
        {
            if (selectedFgdIndex >= 0)
                EditorPrefs.SetString(PREFS_FGD_TYPE, fgdTypes[selectedFgdIndex].AssemblyQualifiedName);
        }

        private void ForceApplyToServer()
        {
            if (currentFgdInstance == null)
            {
                ShowTempStatus("❌ No instance.");
                return;
            }

            try
            {
                if (currentFgdInstance is FGameData fgdBase)
                {
                    fgdBase.Save();
                    ShowTempStatus("✅ Saved.");
                }
            }
            catch (Exception e)
            {
                ShowTempStatus("❌ Save failed: " + e.Message);
                Debug.LogWarning(e);
            }
        }

        private void DrawFGameDataTab()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                var newIdx = EditorGUILayout.Popup(
                    Mathf.Max(0, selectedFgdIndex),
                    fgdTypeNames.Length > 0 ? fgdTypeNames : s_emptyFgdTypeNames,
                    GUILayout.MinWidth(200));

                if (EditorGUI.EndChangeCheck() && fgdTypes.Count > 0)
                {
                    selectedFgdIndex = Mathf.Clamp(newIdx, 0, fgdTypes.Count - 1);
                    SaveSelectedFgdType();
                    LoadOrCreateInstanceForSelected();
                }

                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    if (selectedFgdIndex >= 0) LoadOrCreateInstanceForSelected();
                    ShowTempStatus("✅ Reloaded.");
                }

                if (GUILayout.Button("Force", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    ForceApplyToServer();
                    ShowTempStatus("✅ Applied.");
                }

                GUILayout.FlexibleSpace();
            }

            if (fgdTypes == null || fgdTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("There no instance inherits FGameData<> in domain.", MessageType.Info);
                return;
            }

            if (currentFgdInstance == null)
            {
                EditorGUILayout.HelpBox("Instance has not been initialized yet.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Type: {fgdTypes[selectedFgdIndex].FullName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(EditorApplication.isPlaying ? "Source: runtime instance" : "Source: save on disk", EditorStyles.miniLabel);
            EditorGUILayout.Space();

            using (var sv = new EditorGUILayout.ScrollViewScope(fgdScroll))
            {
                fgdScroll = sv.scrollPosition;
                if (fgdTree != null)
                {
                    EditorGUI.BeginChangeCheck();
                    fgdTree.Draw(false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Repaint();
                    }
                }
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }

            if (EditorApplication.timeSinceStartup >= statusExpireTime)
            {
                statusMessage = "";
            }
        }

        private void ShowTempStatus(string msg)
        {
            statusMessage = msg;
            statusExpireTime = EditorApplication.timeSinceStartup + 2.5f;
        }

        private void ExpandAllFgdTree()
        {
            if (fgdTree == null) return;

            foreach (var property in fgdTree.EnumerateTree(includeChildren: true, onlyVisible: false))
            {
                property.State.Expanded = true;
            }
        }
    }

    public static class FGameDataTypeUtil
    {
        private static readonly Dictionary<Type, string> _cache = new();

        public static void TryGetFgdTypeName(this Type t, out string typeName)
        {
            if (t == null)
            {
                typeName = null;
                return;
            }

            if (_cache.TryGetValue(t, out typeName)) return;

            var attr = t.GetCustomAttribute<FGameDataTypeAttribute>(inherit: false);
            typeName = attr is { Skip: false } ? attr.TypeName : null;
            _cache[t] = typeName;
        }
    }
}
