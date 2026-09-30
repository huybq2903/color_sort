/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-04-20
 */

using System.Net.Http;
using System.Threading.Tasks;
using Falcon.Manager.Shared;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public class GameTemplateTabContent : ITabContent
    {
        private const string URL_FRAMEWORK_DOC =
            "https://puzzle.data4game.com/unity-module/framework-documentation";

        private readonly CMSService _cmsService;
        private readonly GameTemplateVersionService _versionService;
        private readonly GameTemplateImportService _importService;

        private GameTemplateConfig _config;
        private SimplePackageCollection _sharedAssetsConfig;
        private InstalledTemplateInfo _installedInfo;
        private Vector2 _sharedAssetsScrollPosition;
        private Vector2 _templatesScrollPosition;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _showSharedAssets = true;
        private bool _showTemplates = true;

        private IViewController _nextController;
        private EditorWindow _hostWindow;

        public GameTemplateTabContent(CMSService cmsService)
        {
            _cmsService = cmsService;
            _versionService = new GameTemplateVersionService();
            _importService = new GameTemplateImportService();
            _isLoaded = false;
            _isLoading = false;
        }

        public void OnGUI(EditorWindow window)
        {
            _hostWindow = window;

            if (!_isLoaded && !_isLoading)
            {
                _isLoading = true;
                _ = LoadTemplates();
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("<b><color=cyan>Shared Assets & Game Templates</color></b>", Styles.RichText(new RectOffset(0, 0, 0, 5), TextAnchor.MiddleLeft));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Documentation", GUILayout.Width(120), GUILayout.Height(20)))
            {
                Application.OpenURL(URL_FRAMEWORK_DOC);
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(80), GUILayout.Height(20)))
            {
                _isLoaded = false;
                _config = null;
                _sharedAssetsConfig = null;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (_isLoading)
            {
                EditorGUILayout.LabelField("Loading...");
                return;
            }

            _showSharedAssets = EditorGUILayout.Foldout(_showSharedAssets, "Shared-Assets", true, EditorStyles.foldoutHeader);
            
            if (_showSharedAssets)
            {
                int sharedCount = _sharedAssetsConfig?.packages?.Count ?? 0;
                float sharedHeight = sharedCount > 0 ? Mathf.Min(sharedCount * 50f, 200f) : 30f;
                
                _sharedAssetsScrollPosition = EditorGUILayout.BeginScrollView(_sharedAssetsScrollPosition,
                    GUILayout.ExpandWidth(true),
                    GUILayout.Height(sharedHeight));
                
                if (_sharedAssetsConfig != null && _sharedAssetsConfig.packages != null && _sharedAssetsConfig.packages.Count > 0)
                {
                    foreach (var package in _sharedAssetsConfig.packages)
                    {
                        RenderSharedAssetItem(package);
                        GUILayout.Space(5);
                    }
                }
                else
                {
                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.LabelField("No Shared-Assets available.", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            GUILayout.Space(10);

            _showTemplates = EditorGUILayout.Foldout(_showTemplates, "Game Templates", true, EditorStyles.foldoutHeader);
            
            if (_showTemplates)
            {
                int templateCount = _config?.templates?.Count ?? 0;
                float templateHeight = templateCount > 0 ? Mathf.Min(templateCount * 80f, 300f) : 30f;
                
                _templatesScrollPosition = EditorGUILayout.BeginScrollView(_templatesScrollPosition,
                    GUILayout.ExpandWidth(true),
                    GUILayout.Height(templateHeight));
                
                if (_config != null && _config.templates != null && _config.templates.Count > 0)
                {
                    foreach (var template in _config.templates)
                    {
                        RenderTemplateItem(template);
                        GUILayout.Space(5);
                    }
                }
                else
                {
                    EditorGUILayout.BeginVertical("helpbox");
                    EditorGUILayout.LabelField("No game templates available.", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }

        private void RenderSharedAssetItem(SimplePackage package)
        {
            EditorGUILayout.BeginVertical("helpbox", GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{package.displayName}", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label($"{package.version}", GUILayout.Width(60));
            
            if (!string.IsNullOrEmpty(package.date))
            {
                GUILayout.Label($"({package.date})",GUILayout.Width(100));
            }
            
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Install", GUILayout.Width(120), GUILayout.Height(25)))
            {
                InstallSharedAsset(package.fileName);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void RenderTemplateItem(GameTemplatePackage template)
        {
            EditorGUILayout.BeginVertical("helpbox", GUILayout.ExpandWidth(true));

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{template.displayName}", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Label($"{template.version}", GUILayout.Width(60));
            
            if (!string.IsNullOrEmpty(template.date))
            {
                GUILayout.Label($"({template.date})",GUILayout.Width(100));
            }
            
            GUILayout.FlexibleSpace();

            var state = _versionService.GetState(template, _installedInfo);
            RenderStateButton(template, state);

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(template.changelog))
            {
                GUILayout.Label("Changelog:");
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(template.changelog, EditorStyles.wordWrappedLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void RenderStateButton(GameTemplatePackage template, TemplateState state)
        {
            switch (state)
            {
                case TemplateState.NotInstalled:
                    if (GUILayout.Button($"Install {template.version}", GUILayout.Width(120), GUILayout.Height(25)))
                    {
                        InstallTemplate(template);
                    }

                    break;

                case TemplateState.Installed:
                    GUI.enabled = false;
                    GUILayout.Button($"Installed {template.version}", GUILayout.Width(120), GUILayout.Height(25));
                    GUI.enabled = true;
                    break;

                case TemplateState.NeedsUpdate:
                    if (_installedInfo != null)
                    {
                        GUILayout.Label($"Current: {_installedInfo.version}", GUILayout.Width(100));
                    }

                    if (GUILayout.Button($"Update to {template.version}", GUILayout.Width(120), GUILayout.Height(25)))
                    {
                        InstallTemplate(template);
                    }

                    break;
            }
        }

        private async void InstallTemplate(GameTemplatePackage template)
        {
            var success = await _importService.DownloadAndImport(template);

            if (success)
            {
                _installedInfo = InstalledTemplateRepository.Load();
                _hostWindow?.Repaint();
            }
        }

        private void InstallSharedAsset(string uri)
        {
            SimplePackageImporter.DownloadAndImport("Shared-Assets", uri, new OnlyNewImporter())
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError(task.Exception);
                    }
                    else
                    {
                        EditorApplication.delayCall += () => _hostWindow?.Repaint();
                    }
                });
        }

        private async Task LoadTemplates()
        {
            GameTemplateConfig config = new GameTemplateConfig();
            SimplePackageCollection sharedConfig = new SimplePackageCollection();
            InstalledTemplateInfo installed = null;

            using (var client = new HttpClient())
            {
                try
                {
                    var templatesUrl = $"{Configuration.GAME_TEMPLATE_CDN_BASE}/template-config.json";
                    var templatesResponse = await client.GetAsync(templatesUrl);
                    templatesResponse.EnsureSuccessStatusCode();
                    var templatesJson = await templatesResponse.Content.ReadAsStringAsync();
                    config = JsonConvert.DeserializeObject<GameTemplateConfig>(templatesJson) ?? new GameTemplateConfig();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to load game templates: {e.Message}");
                }

                try
                {
                    var sharedUrl = "https://jp-osa-1.linodeobjects.com/falcon-framework-libs/shared-assets/shared-assets-config.json";
                    var sharedResponse = await client.GetAsync(sharedUrl);
                    sharedResponse.EnsureSuccessStatusCode();
                    var sharedJson = await sharedResponse.Content.ReadAsStringAsync();
                    sharedConfig = JsonUtility.FromJson<SimplePackageCollection>(sharedJson) ?? new SimplePackageCollection();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to load shared assets: {e.Message}");
                }
            }

            installed = InstalledTemplateRepository.Load();

            EditorApplication.delayCall += () =>
            {
                _config = config;
                _sharedAssetsConfig = sharedConfig;
                _installedInfo = installed;
                _isLoading = false;
                _isLoaded = true;
                _hostWindow?.Repaint();
            };
        }

        public bool TryGetNextController(out IViewController controller)
        {
            controller = _nextController;
            if (_nextController != null)
            {
                _nextController = null;
                return true;
            }

            return false;
        }
    }
}
