 /*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-04-24
     */


using System.Linq;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    using System.Collections.Generic;

    public partial class ModuleSettingsWindow : EditorWindow
    {
        private const float MODULES_WIDTH = 200f;
        private const float CONFIGS_WIDTH = 250f;
        private const float EDITOR_WIDTH  = 450f;
        
        private UnityEditor.Editor _cachedEditor;
        private Object _cachedInstance;
        private string _cachedPath;

        private void RenderModulesPanel()
        {
            // Search targets configs, not the module list — show all modules always.
            var modules = _service.GetAllModules();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(MODULES_WIDTH));
            GUILayout.Label("Modules", EditorStyles.boldLabel);

            _modulesScroll = EditorGUILayout.BeginScrollView(_modulesScroll, GUILayout.Width(MODULES_WIDTH - 16));

            foreach (var module in modules)
            {
                var hasConfigs = _service.HasConfigs(module.Name);
                var isSelected = _selectedModuleName == module.Name;
                var guiColor = hasConfigs ? Color.white : new Color(1, 1, 1, 0.4f);
                
                GUI.color = guiColor;
                var selected = EditorGUILayout.ToggleLeft(
                    module.DisplayName + (hasConfigs ? "" : " (no configs)"),
                    isSelected,
                    EditorStyles.label 
                );
                GUI.color = Color.white;

                if (selected && !isSelected)
                {
                    _selectedModuleName = module.Name;
                    _selectedAsset = null;
                    InvalidateEditor();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void InvalidateEditor()
        {
            if (_cachedEditor != null)
            {
                Object.DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }
            _cachedInstance = null;
            _cachedPath = null;
        }

        private void RenderConfigsPanel()
        {
            // When there's a search term, search configs across ALL modules (no need to
            // pick a module first); otherwise show the selected module's configs.
            var searching = !string.IsNullOrEmpty(_searchText);
            var configs = searching
                ? FilterConfigs(_service.GetAllAssets())
                : string.IsNullOrEmpty(_selectedModuleName)
                    ? new List<SOAssetInfo>()
                    : _service.GetModuleAssets(_selectedModuleName);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(CONFIGS_WIDTH));
            GUILayout.Label(searching ? $"Configs (search: {configs.Count})" : "Configs", EditorStyles.boldLabel);

            _configsScroll = EditorGUILayout.BeginScrollView(_configsScroll, GUILayout.Width(CONFIGS_WIDTH - 16));

            foreach (var asset in configs)
            {
                var isSelected = _selectedAsset == asset;
                var label = searching ? $"{asset.Name}  ·  {ModuleNameOf(asset)}" : asset.Name;
                var selected = EditorGUILayout.ToggleLeft(
                    label,
                    isSelected,
                    EditorStyles.label
                );

                if (selected && _selectedAsset != asset)
                {
                    _selectedAsset = asset;
                    InvalidateEditor();
                }
            }

            if (configs.Count == 0)
            {
                if (searching)
                    EditorGUILayout.LabelField("(No configs match search)", EditorStyles.wordWrappedLabel);
                else if (!string.IsNullOrEmpty(_selectedModuleName))
                    EditorGUILayout.LabelField("(No configs found)", EditorStyles.wordWrappedLabel);
                else
                    EditorGUILayout.LabelField("(Select a module, or type to search all)", EditorStyles.wordWrappedLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private List<SOAssetInfo> FilterConfigs(List<SOAssetInfo> configs)
        {
            if (string.IsNullOrEmpty(_searchText))
                return configs;
            return configs.Where(c => c.Name.IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private const string MODULES_ROOT = "Assets/FalconAssets/Modules/";

        private static string ModuleNameOf(SOAssetInfo asset)
        {
            var path = asset.AssetPath;
            if (string.IsNullOrEmpty(path) || !path.StartsWith(MODULES_ROOT, System.StringComparison.Ordinal))
                return "";
            var rest = path.Substring(MODULES_ROOT.Length);
            var slash = rest.IndexOf('/');
            return slash >= 0 ? rest.Substring(0, slash) : rest;
        }

        private void RenderEditorPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(EDITOR_WIDTH));
            GUILayout.Label("Editor", EditorStyles.boldLabel);

            if (_selectedAsset == null || _selectedAsset.Instance == null)
            {
                EditorGUILayout.HelpBox("Select a config to edit its properties", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Check if asset still exists
            if (!AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_selectedAsset.AssetPath))
            {
                EditorGUILayout.HelpBox("Asset not found or deleted", MessageType.Warning);
                _selectedAsset = null;
                EditorGUILayout.EndVertical();
                return;
            }

            // Always reload from disk to get fresh instance
            var path = _selectedAsset.AssetPath;
            var freshObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            
            if (freshObj == null)
            {
                EditorGUILayout.HelpBox("Asset instance is null", MessageType.Error);
                EditorGUILayout.EndVertical();
                return;
            }

            // Update the selected asset instance
            _selectedAsset.Instance = freshObj;

            // Buttons at top
            EditorGUILayout.Space(2, true);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Ping", EditorStyles.toolbarButton))
            {
                Selection.activeObject = freshObj;
                EditorGUIUtility.PingObject(freshObj);
            }

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                freshObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                _selectedAsset.Instance = freshObj;
                InvalidateEditor();
            }

            EditorGUILayout.EndHorizontal();

            _editorScroll = EditorGUILayout.BeginScrollView(_editorScroll);

            // Recreate editor if needed
            if (_cachedEditor == null || _cachedInstance != freshObj || _cachedPath != path)
            {
                if (_cachedEditor != null)
                {
                    Object.DestroyImmediate(_cachedEditor);
                    _cachedEditor = null;
                }
                
                _cachedEditor = UnityEditor.Editor.CreateEditor(freshObj);
                _cachedInstance = freshObj;
                _cachedPath = path;
            }

            if (_cachedEditor != null)
            {
                // Render the default inspector (uses custom editor if available)
                _cachedEditor.OnInspectorGUI();
            }
            else
            {
                // Fallback: use SerializedObject directly
                var so = new SerializedObject(freshObj);
                so.Update();
                var prop = so.GetIterator();
                while (prop.Next(true))
                {
                    if (prop.propertyType == SerializedPropertyType.Generic && prop.name == "m_Script")
                        continue;
                    EditorGUILayout.PropertyField(prop, true);
                }
                so.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void OnDestroy()
        {
            InvalidateEditor();
            _serviceInitialized = false;
        }
    }
}