/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.External
{
    using System.Threading.Tasks;

    public class PackageExportController : ACMSValidatedController
    {
        private CMSService           _cmsService;
        private PackageExportService _service;
        
        private List<AuthRegistryEntry> _packages;
        private List<AuthRegistryEntry> _searchedPackages;

        private Vector2         _scrollPosition;
        private string          _searchKeyword = "";
        private IViewController _nextController;
        
        public override void SetCMSService(CMSService service)
        {
            _cmsService = service;
            _service    = new PackageExportService(service);
            
            Init().ContinueWith(t =>
            {
                Debug.Log("Initialize packages");
            });
            
            PackageInfoEditorWindow.OnUpdatePackage -= Refresh;
            PackageInfoEditorWindow.OnUpdatePackage += Refresh;
        }
        
        private async Task Init()
        {
            _packages         = new List<AuthRegistryEntry>();
            _searchedPackages = new List<AuthRegistryEntry>();

            var remoteDict = await _cmsService.GetUserModules();
            
            var resultDict = new Dictionary<string, AuthRegistryEntry>();
            var localDict  = LocalPackageRepository.Packages;

            foreach (var kvp in remoteDict)
            {
                var remote = kvp.Value;
                if (localDict.ContainsKey(kvp.Key))
                {
                    var local  = localDict[kvp.Key];
                    
                    resultDict.Add(kvp.Key, new AuthRegistryEntry()
                    {
                        name             = kvp.Key,
                        displayName      = remote.displayName,
                        version          = local.version,
                        latest           = remote.latest,
                        description      = local.description,
                        author           = local.author ?? new(),
                        asmdefReferences = local.asmdefReferences ?? new(),
                        packagePath      = local.packagePath,
                        packageExists    = true,
                        dependencies     = local.dependencies ?? new(),
                        versions         = remote.versions ?? new(),
                    });
                }
                else
                {
                    resultDict.Add(kvp.Key, new AuthRegistryEntry()
                    {
                        name             = kvp.Key,
                        displayName      = remote.displayName,
                        version          = remote.latest,
                        latest           = remote.latest,
                        description      = remote.description,
                        author           = new(),
                        asmdefReferences = new(),
                        packagePath      = string.Empty,
                        packageExists    = false,
                        dependencies     = new(),
                        versions         = remote.versions ?? new(),
                    });
                }
            }
            
            _packages = resultDict.Values
                .OrderBy(package => package.name.StartsWith("falcon.modules.core") ? 0 : 1)
                .ThenBy(package => package.name.StartsWith("falcon.helpers") ? 0 : 1)
                .ThenBy(package => package.name.StartsWith("falcon.manager") ? 0 : 1)
                .ThenBy(package => package.name.StartsWith("falcon.tools") ? 0 : 1)
                .ThenBy(package => package.name).ToList();
        }

        public override void Edit(EditorWindow window)
        {
            EditorGUILayout.Space(5);
            GUIHorizon(() =>
            {
                EditorGUI.BeginChangeCheck();
                _searchKeyword = EditorGUILayout.TextField($"Search ({_packages.Count} modules)", _searchKeyword);

                GUILayout.Space(5);
                if (GUILayout.Button("Refresh", GUILayout.Width(80), GUILayout.Height(19)))
                {
                    Refresh();
                    return;
                }
                
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_NavMeshAgent Icon"), GUILayout.Width(30), GUILayout.Height(19)))
                {
                    if (EditorUtility.DisplayDialog("Warning", "You're about to log out. Continue?", "Log out", "Cancel"))
                    {
                        AuthKeyRepository.DeleteKey();
                        _nextController = new AuthController<PackageExportController>();
                    }
                }
                
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Linked@2x"), GUILayout.Width(30), GUILayout.Height(19)))
                {
                    Application.OpenURL(Configuration.CMS_WEB_URL);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    Search();
                }
            });
            
            EditorGUILayout.Space(5);

            var packagesToDisplay = string.IsNullOrEmpty(_searchKeyword) ? _packages : _searchedPackages;
            if (packagesToDisplay.Count == 0)
            {
                EditorGUILayout.LabelField("No packages found!");
                DrawCreation();
                return;
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition,
                false,
                true,
                GUILayout.ExpandWidth(false),
                GUILayout.ExpandHeight(true)
            );

            foreach (var package in packagesToDisplay)
            {
                GUIHorizon(() =>
                {
                    DisplayPackageInfo(package);
                    DisplayPackageCTA(package);
                }, EditorStyles.helpBox);
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
            
            DrawCreation();
        }

        private void DrawCreation()
        {
            if (GUILayout.Button("Create New Module", GUILayout.Height(25)))
            {
                CreateModuleOptionWindow.OpenWindow();
            }
        }

        private void Search()
        {
            _searchedPackages.Clear();
            if (!string.IsNullOrEmpty(_searchKeyword))
            {
                _searchedPackages.AddRange(_packages.Where(p =>
                    p.name.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.displayName.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0
                ));
            }
        }

        private void DisplayPackageInfo(AuthRegistryEntry package)
        {
            GUIVertical(() =>
            {
                EditorGUILayout.LabelField($"{package.displayName} ({package.version})", EditorStyles.whiteBoldLabel);

                if (GUILayout.Button($">> {package.name}", EditorStyles.miniLabel, GUILayout.Width(275),  GUILayout.Height(16)))
                {
                    GUIUtility.systemCopyBuffer = package.name;
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent($"Package Id Copied\n{package.name}"));
                }
                
                if (package.asmdefReferences.Count <= 0) return;
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Asmdef dependencies:", WindowStyles.TextColor(WindowStyles.kYellow));
                foreach (var dep in package.asmdefReferences)
                {
                    if (PackageExportService.TryGetPackageByAsmdefDependency(dep, out var found))
                        EditorGUILayout.LabelField($"  - {found.name}@{found.version}",
                            WindowStyles.TextMiniColor(WindowStyles.kGreen));
                    else
                        EditorGUILayout.LabelField($"  - {PackageExportService.GetTruePackageName(dep)}",
                            WindowStyles.TextMiniColor(WindowStyles.kRed));
                }
            });
        }

        private async void Refresh()
        {
            LocalPackageRepository.Refresh();
            await Init();
            
            Search();
        }

        private async void DisplayPackageCTA(AuthRegistryEntry package)
        {
            if (!package.packageExists)
            {
                GUILayout.Label("Local Package Not Found", WindowStyles.TextAlignment(TextAnchor.MiddleRight));
                return;
            }

            bool asmdefValid = package.asmdefReferences.All(dep => PackageExportService.TryGetPackageByAsmdefDependency(dep, out var found));
            if (!asmdefValid)
            {
                GUILayout.Label("Asmdef Dependencies Not Valid", WindowStyles.TextAlignment(TextAnchor.MiddleRight));
                return;
            }

            if (ConfigExporterService.IsExistConfigs(package.packagePath))
            {
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Package Manager@2x"), GUILayout.Width(30), GUILayout.Height(30)))
                {
                    ConfigExporterWindow.ShowWindow(package);
                }
            }

            bool existed = package.version.Equals(package.latest);
            var  latest  = package.latest;

            string content = existed ? $"Uploaded {latest}" : $"Upload {package.version}";

            GUI.enabled = existed;

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_editicon.sml"), GUILayout.Width(30), GUILayout.Height(30)))
            {
                PackageInfoEditorWindow.OpenWindow(package);
            }

            GUI.enabled = true;

            GUI.enabled = !existed;
            if (GUILayout.Button(content, GUILayout.Width(115), GUILayout.Height(30)))
            {
                await _service.ExportPackage(package).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError(task.Exception);
                    }
                });
                
                Refresh();
            }

            GUI.enabled = true;
        }

        public override bool TryMoveNextController(out IViewController viewController)
        {
            viewController = _nextController;
            return viewController != null;
        }
    }
}