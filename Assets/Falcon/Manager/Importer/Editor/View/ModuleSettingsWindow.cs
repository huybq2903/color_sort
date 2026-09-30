 /*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-04-24
     */


using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public partial class ModuleSettingsWindow : EditorWindow
    {
        private ModuleSettingsService _service;
        private Vector2               _modulesScroll;
        private Vector2               _configsScroll;
        private Vector2               _editorScroll;
        private string                _selectedModuleName;
        private SOAssetInfo           _selectedAsset;
        private string                _searchText = "";
        private bool                  _serviceInitialized;

        private enum Tab { General, ModuleSettings }
        private Tab _selectedTab = Tab.ModuleSettings;
        private static readonly string[] TabLabels = { "General", "Module Settings" };

        [MenuItem("Falcon/Manager/Unify Module Settings", priority = 701)]
        public static void ShowWindow()
        {
            var window = GetWindow<ModuleSettingsWindow>("Unify Module Settings");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        private void InitializeService()
        {
            // _service is a plain class (not serialized), so a domain reload — e.g. after
            // entering/exiting Play mode — nulls it while the serialized _serviceInitialized
            // flag stays true. Re-create and Reload whenever _service is missing so the
            // module list never comes back null after playing.
            if (_serviceInitialized && _service != null) return;
            _service = new ModuleSettingsService();
            _service.Reload();
            _serviceInitialized = true;
        }

        private void OnGUI()
        {
            InitializeService();

            RenderToolbar();
            RenderTabBar();

            switch (_selectedTab)
            {
                case Tab.General:
                    RenderGeneralTab();
                    break;

                case Tab.ModuleSettings:
                    EditorGUILayout.BeginHorizontal();
                    RenderModulesPanel();
                    RenderConfigsPanel();
                    RenderEditorPanel();
                    EditorGUILayout.EndHorizontal();
                    break;
            }
        }

        private void RenderToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton))
            {
                _service.Reload();
                _selectedModuleName = null;
                _selectedAsset = null;
            }

            EditorGUILayout.Space();

            if (_selectedTab == Tab.ModuleSettings)
            {
                GUILayout.Label("Search", GUILayout.Width(45));
                _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField,
                    GUILayout.MinWidth(200));
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void RenderTabBar()
        {
            _selectedTab = (Tab)GUILayout.Toolbar((int)_selectedTab, TabLabels);
        }

    }
}