/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-02-25
     */


using System.Collections.Generic;
using System.Linq;
using System.Text;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public class QuickStartTabContent : ITabContent
    {
        private readonly CMSService _cmsService;
        private readonly PackageManageService _manageService;
        private readonly Dictionary<string, FalconPackageInfo> _packageInfos;
        private readonly QuickStartConfig _config;

        private readonly Dictionary<string, bool> _selectedModules = new();
        private readonly Dictionary<string, bool> _groupFoldStates = new();
        private readonly HashSet<string> _configModuleNames = new();

        // rebuilt each OnGUI from current selection
        private Dictionary<string, List<string>> _depReasons = new();

        private IViewController _nextController;
        private Vector2 _scrollPosition;

        public QuickStartTabContent(
            CMSService cmsService,
            PackageManageService manageService,
            Dictionary<string, FalconPackageInfo> packageInfos)
        {
            _cmsService    = cmsService;
            _manageService = manageService;
            _packageInfos  = packageInfos;

            _config = LoadConfig();
            if (_config == null) return;

            foreach (var group in _config.groups)
            {
                _groupFoldStates[group.groupName] = true;
                foreach (var moduleName in group.moduleNames)
                {
                    if (!_packageInfos.ContainsKey(moduleName)) continue;
                    _selectedModules[moduleName] = true;
                    _configModuleNames.Add(moduleName);
                }
            }

            RebuildDependencies();
        }

        private void RebuildDependencies()
        {
            _depReasons = new Dictionary<string, List<string>>();
            var visited = new HashSet<string>();

            foreach (var moduleName in _configModuleNames)
            {
                if (!_selectedModules.TryGetValue(moduleName, out var selected) || !selected) continue;
                if (!_packageInfos.TryGetValue(moduleName, out var info)) continue;
                CollectDeps(info, info.DisplayName, visited);
            }

            // remove dep selections that are no longer needed
            var staleDeps = _selectedModules.Keys
                .Where(k => !_configModuleNames.Contains(k) && !_depReasons.ContainsKey(k))
                .ToList();
            foreach (var key in staleDeps)
                _selectedModules.Remove(key);
        }

        private void CollectDeps(FalconPackageInfo info, string rootDisplayName, HashSet<string> visited)
        {
            foreach (var (depName, _) in info.RemoteDependencies)
            {
                if (_configModuleNames.Contains(depName)) continue;
                if (!_packageInfos.TryGetValue(depName, out var depInfo)) continue;

                if (!_depReasons.ContainsKey(depName))
                    _depReasons[depName] = new List<string>();

                if (!_depReasons[depName].Contains(rootDisplayName))
                    _depReasons[depName].Add(rootDisplayName);

                _selectedModules.TryAdd(depName, true);

                if (visited.Add(depName))
                    CollectDeps(depInfo, rootDisplayName, visited);
            }
        }

        private static QuickStartConfig LoadConfig()
        {
            var guids = AssetDatabase.FindAssets("t:QuickStartConfig");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<QuickStartConfig>(path);
        }

        public void OnGUI(EditorWindow window)
        {
            if (_config == null || _config.groups.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "QuickStartConfig asset not found. Create one via Assets > Create > Falcon > Quick Start Config.",
                    MessageType.Warning);
                return;
            }

            RebuildDependencies();

            GUILayout.Space(5);
            
            GUILayout.Label("<b><color=cyan>QUICK START</color></b>", Styles.RichText(new RectOffset(0, 0, 5, 5), TextAnchor.MiddleCenter));
            GUILayout.Space(3);
            EditorGUILayout.LabelField(
                "The following modules are essential for early developing project.\nPlease select fitting modules for your project:",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(8);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            foreach (var group in _config.groups)
            {
                RenderGroup(group);
            }

            if (_depReasons.Count > 0)
            {
                GUILayout.Space(5);
                RenderDependenciesGroup();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(5);
            RenderBottomBar();
            GUILayout.Space(5);
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

        private void RenderGroup(QuickStartConfig.ModuleGroup group)
        {
            _groupFoldStates.TryAdd(group.groupName, true);

            var validModules = group.moduleNames
                .Where(name => _packageInfos.ContainsKey(name))
                .Select(name => _packageInfos[name])
                .ToList();

            if (validModules.Count == 0) return;

            EditorGUILayout.BeginVertical("helpbox");

            var style = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            _groupFoldStates[group.groupName] = EditorGUILayout.Foldout(
                _groupFoldStates[group.groupName],
                $"{group.groupName} ({validModules.Count} modules)", true, style);

            if (_groupFoldStates[group.groupName])
            {
                EditorGUI.indentLevel++;
                foreach (var info in validModules)
                {
                    RenderModuleToggle(info);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private void RenderDependenciesGroup()
        {
            const string groupKey = "Dependencies (auto-resolved)";
            _groupFoldStates.TryAdd(groupKey, true);

            var depInfos = _depReasons.Keys
                .Where(name => _packageInfos.ContainsKey(name))
                .Select(name => _packageInfos[name])
                .ToList();

            if (depInfos.Count == 0) return;

            EditorGUILayout.BeginVertical("helpbox");

            var style = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            _groupFoldStates[groupKey] = EditorGUILayout.Foldout(
                _groupFoldStates[groupKey],
                $"{groupKey} ({depInfos.Count} modules)", true, style);

            if (_groupFoldStates[groupKey])
            {
                EditorGUI.indentLevel++;
                foreach (var info in depInfos)
                {
                    RenderModuleToggle(info, _depReasons[info.Name]);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private void RenderModuleToggle(FalconPackageInfo info, List<string> reasons = null)
        {
            _selectedModules.TryAdd(info.Name, true);

            EditorGUILayout.BeginHorizontal();

            bool unsupported = string.IsNullOrEmpty(info.LatestVersion);

            if (unsupported)
            {
                GUI.enabled = false;
                EditorGUILayout.ToggleLeft($"{info.DisplayName}  (no longer supported)", false,  GUILayout.Width(350));
                GUI.enabled = true;
            }
            else
            {
                if (info.Installed && info.IsInstallLatestVersion())
                {
                    GUI.enabled = false;
                    EditorGUILayout.ToggleLeft($"{info.DisplayName}  (latest)", false,  GUILayout.Width(350));
                    GUI.enabled = true;
                }
                else
                {
                    var label = info.Installed
                        ? $"{info.DisplayName}  (update: {info.InstalledVersion} -> {info.LatestVersion})"
                        : $"{info.DisplayName}  ({info.LatestVersion})";

                    _selectedModules[info.Name] = EditorGUILayout.ToggleLeft(label, _selectedModules[info.Name], GUILayout.Width(350));
                }
            }

            if (reasons is { Count: > 0 })
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"  <- {reasons.Count} modules required", EditorStyles.miniLabel);

                if (GUILayout.Button("ii", GUILayout.Width(20), GUILayout.Height(15)))
                {
                    ShowDependenciesDialog(info.DisplayName, reasons);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void ShowDependenciesDialog(string moduleName, List<string> reasons)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{moduleName} is required by:");
            foreach (var reason in reasons)
            {
                sb.AppendLine($"  - {reason}");
            }

            EditorUtility.DisplayDialog("Dependencies", sb.ToString(), "Ok");
        }

        private void RenderBottomBar()
        {
            var selectedInfos = GetSelectedInstallableInfos();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = selectedInfos.Count > 0;
            if (GUILayout.Button($"Install ({selectedInfos.Count})", GUILayout.Width(150), GUILayout.Height(35)))
            {
                var names = string.Join("\n", selectedInfos.Select(i => $"  - {i.DisplayName}"));
                if (EditorUtility.DisplayDialog(
                        "Quick Start Install",
                        $"The following {selectedInfos.Count} modules will be installed/updated:\n{names}",
                        "Install", "Cancel"))
                {
                    _nextController = new QuickStartInstallController(_cmsService, _manageService, selectedInfos);
                }
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private List<FalconPackageInfo> GetSelectedInstallableInfos()
        {
            var seen = new HashSet<string>();
            var result = new List<FalconPackageInfo>();

            foreach (var (name, selected) in _selectedModules)
            {
                if (!selected) continue;
                if (!seen.Add(name)) continue;
                if (!_packageInfos.TryGetValue(name, out var info)) continue;
                if (string.IsNullOrEmpty(info.LatestVersion)) continue;
                if (info.Installed && info.IsInstallLatestVersion()) continue;
                result.Add(info);
            }

            return result;
        }
    }
}
