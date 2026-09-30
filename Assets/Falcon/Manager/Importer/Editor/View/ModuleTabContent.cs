/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-02-25
     */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public class ModuleTabContent : ITabContent
    {
        private readonly CMSService           _cmsService;
        private readonly PackageManageService _manageService;
        
        private readonly List<FalconPackageInfo> _packageInfos;
        private readonly IEnumerable<IGrouping<string, FalconPackageInfo>> _packageGroups;
        
        private List<FalconPackageInfo> _searchedPackageInfos;
        
        private readonly Texture2D _trashIcon;
        private readonly Dictionary<string, bool> _packageFoldStates = new();

        private IViewController _nextController;
        private string _searchKeyword = "";
        private Vector2 _scrollPosition;

        public ModuleTabContent(
            CMSService cmsService,
            PackageManageService manageService,
            List<FalconPackageInfo> packageInfos,
            IEnumerable<IGrouping<string, FalconPackageInfo>> packageGroups,
            Texture2D trashIcon)
        {
            _cmsService   = cmsService;
            _manageService = manageService;
            _packageInfos = packageInfos;
            _packageGroups = packageGroups;
            _trashIcon = trashIcon;
            _searchedPackageInfos = new();

            _packageFoldStates.Clear();
            foreach (var packageInfo in _packageInfos)
            {
                _packageFoldStates[packageInfo.Name] = false;
            }
        }

        private void ApplySearch()
        {
            _searchedPackageInfos.Clear();
            if (!string.IsNullOrEmpty(_searchKeyword))
            {
                _searchedPackageInfos.AddRange(_packageInfos.Where(p =>
                    p.Name.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.DisplayName.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0
                ));
            }
        }

        public void OnGUI(EditorWindow window)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Loaded {_packageInfos.Count} modules");
            EditorGUI.BeginChangeCheck();
            _searchKeyword = EditorGUILayout.TextField("", _searchKeyword);
            if (EditorGUI.EndChangeCheck())
            {
                ApplySearch();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));

            if (!string.IsNullOrEmpty(_searchKeyword))
            {
                if (_searchedPackageInfos.Count > 0)
                {
                    RenderInfos("Searched", _searchedPackageInfos);
                }
                else
                {
                    EditorGUILayout.LabelField("No packages found!");
                }

                EditorGUILayout.EndScrollView();
                return;
            }

            foreach (var group in _packageGroups)
            {
                RenderGroup(group);
            }

            EditorGUILayout.EndScrollView();
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

        private void RenderInfos(string infoName, List<FalconPackageInfo> infos)
        {
            if (infos.Count <= 0) return;
            GUILayout.Label($"{infoName} modules ({infos.Count})", Styles.TextAlignment(TextAnchor.MiddleCenter));
            EditorGUILayout.BeginVertical("helpbox");
            foreach (var packageInfo in infos)
            {
                RenderModuleItem(packageInfo);
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void RenderGroup(IGrouping<string, FalconPackageInfo> group)
        {
            RenderInfos(group.Key, group.ToList());
        }

        private void RenderModuleItem(FalconPackageInfo info)
        {
            EditorGUILayout.BeginVertical("box");
            
            var style = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };

            var label = info.DisplayName;

            EditorGUILayout.BeginHorizontal();
            {
                _packageFoldStates[info.Name] = EditorGUILayout.Foldout(_packageFoldStates[info.Name], label, true, style);
                GUILayout.FlexibleSpace();
                
                if (!info.Installed)
                    RenderNotInstalledModule(info);
                else if (info.LatestVersion == null)
                    RenderNotSupportedModule(info);
                else if (!info.IsInstallLatestVersion())
                    RenderOldModule(info);
                else
                    RenderNewestModule(info);
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(2);
            if (_packageFoldStates[info.Name])
            {
                EditorGUILayout.BeginVertical();
                var padding = new RectOffset(15, 0, 0, 0);

                if (!string.IsNullOrEmpty(info.Author))
                {
                    EditorGUILayout.LabelField($"<b>Author:</b> {info.Author} - <i>{info.AuthorEmail}</i>", Styles.RichText(padding, TextAnchor.MiddleLeft));
                }

                EditorGUILayout.LabelField($"<b>Description:</b> {info.Description}", Styles.RichText(padding, TextAnchor.MiddleLeft));

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(18);
                
                if (GUILayout.Button(EditorGUIUtility.IconContent("TextAsset Icon"), GUILayout.Width(30), GUILayout.Height(18)))
                {
                    PackageInfoContentShowingEditor.Open(info.DisplayName, info.Name, "README");
                }
                
                GUILayout.Space(3);
                if (GUILayout.Button(EditorGUIUtility.IconContent("console.infoicon.sml"), GUILayout.Width(30), GUILayout.Height(18)))
                {
                    PackageInfoContentShowingEditor.Open(info.DisplayName, info.Name, "CHANGELOG");
                }

                GUILayout.Space(3);
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_LODGroup Icon"), GUILayout.Width(30), GUILayout.Height(18)))
                {
                    ShowDependencies(info);
                }

                if (info.Installed)
                {
                    GUILayout.Space(3);
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Folder Icon"), GUILayout.Width(30), GUILayout.Height(18)))
                    {
                        var folderObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(info.InstalledPath);
                        if (folderObject) EditorGUIUtility.PingObject(folderObject);
                        
#if UNITY_EDITOR_WIN
                        System.Diagnostics.Process.Start("explorer.exe", info.InstalledPath);
#endif
                    }
                }

                if (info.LatestVersionInfo != null && !string.IsNullOrEmpty(info.LatestVersionInfo.date))
                {
                    EditorGUILayout.LabelField($"<i>Last Updated: {info.LatestVersionInfo.date}</i>", Styles.RichText(padding, TextAnchor.LowerRight));
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void ShowDependencies(FalconPackageInfo info)
        {
            var builder = new StringBuilder();
            foreach (var kvp in info.RemoteDependencies)
            {
                builder.AppendLine($"<i> - {kvp.Key}@{kvp.Value}</i>");
            }
            
            ContentShowingEditor.Open(info.DisplayName, "DEPENDENCIES", builder.Length > 0 ? builder.ToString() : " --- none ---");
        }

        private void RenderNotInstalledModule(FalconPackageInfo falconPackageInfo)
        {
            if (GUILayout.Button($"Install {falconPackageInfo.LatestVersion}", GUILayout.Width(110), GUILayout.Height(20)))
                AskThenInstall(falconPackageInfo);

            GUI.enabled = false;
            RenderUninstallButton(falconPackageInfo);
            GUI.enabled = true;
        }

        private void RenderOldModule(FalconPackageInfo falconPackageInfo)
        {
            GUILayout.Label($"Current: {falconPackageInfo.InstalledVersion}");
            if (GUILayout.Button($"Update to {falconPackageInfo.LatestVersion}", GUILayout.Width(110), GUILayout.Height(20)))
                AskThenInstall(falconPackageInfo);
            RenderUninstallButton(falconPackageInfo);
        }

        private void RenderNewestModule(FalconPackageInfo falconPackageInfo)
        {
            GUILayout.Label($"Current: {falconPackageInfo.InstalledVersion}");
            GUI.enabled = false;
            if (GUILayout.Button("Up to date", GUILayout.Width(110), GUILayout.Height(20)))
                AskThenInstall(falconPackageInfo);
            GUI.enabled = true;
            RenderUninstallButton(falconPackageInfo);
        }

        private static void RenderNotSupportedModule(FalconPackageInfo falconPackageInfo)
        {
            GUILayout.Label($"Current: {falconPackageInfo.InstalledVersion}");
            GUILayout.Label(" No longer supported ", Styles.TextAlignment(TextAnchor.MiddleRight));
        }

        private void AskThenInstall(FalconPackageInfo falconPackageInfo)
        {
            _nextController = new ModuleAskingController(_cmsService, _manageService, falconPackageInfo);
        }

        private void RenderUninstallButton(FalconPackageInfo falconPackageInfo)
        {
            void Uninstall()
            {
                PackageManageService.Uninstall(falconPackageInfo);
                var controller = new LoadingController();
                controller.SetCMSService(_cmsService);
                _nextController = controller;
            }
            
            if (!GUILayout.Button(_trashIcon, GUILayout.Width(20), GUILayout.Height(20))) return;
            var usingDependencies = _manageService.GetUsingDependencies(falconPackageInfo);
            if (usingDependencies.Count > 0)
            {
                var requireModuleString = new StringBuilder();
                requireModuleString
                    .Append("The following modules are requiring " + falconPackageInfo.DisplayName + ":").AppendLine();
                foreach (var info in usingDependencies)
                    requireModuleString.Append("  - ").Append(info.DisplayName).AppendLine();
                requireModuleString.Append("Those will get errors when this package is deleted!");
                if (EditorUtility.DisplayDialog("Dependency detected!!!", requireModuleString.ToString(), "Delete Anyway", "Cancel"))
                {
                    Uninstall();
                }
            }
            else
            {
                if (EditorUtility.DisplayDialog("Warning", "Are you sure. This action cannot be undone!", "Lets Delete", "Cancel"))
                {
                    Uninstall();
                }
            }
        }
    }
}
