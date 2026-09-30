/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public class ModulesShowingController : AViewController
    {
        private readonly CMSService           _cmsService;
        private readonly PackageManageService _manageService;
        
        private readonly List<FalconPackageInfo> _packageInfos;
        
        private readonly Texture2D _trashIcon;
        private IViewController _nextController;

        private int _selectedTab;
        private readonly string[] _tabNames = { "Modules", "Quick Start", "Game Template" };
        
        private readonly ModuleTabContent    _moduleTab;
        private readonly QuickStartTabContent _quickStartTab;
        private readonly GameTemplateTabContent _gameTemplateTab;

        public ModulesShowingController(CMSService cmsService, Dictionary<string, FalconPackageInfo> infos)
        {
            _cmsService   = cmsService;
            _packageInfos = infos.Values.ToList();
            _packageInfos.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            var packageGroups = _packageInfos.GroupBy(GetKeyword)
                .OrderBy(g => g.Key.Equals("Manager") ? 0 : 1)
                .ThenBy(g => g.Key.Equals("Helpers") ? 0 : 1)
                .ThenBy(g => g.Key.Equals("Core") ? 0 : 1)
                .ThenBy(g => g.Key);
            
            _trashIcon = new Texture2D(2, 2);
            var file = Directory.GetFiles(Application.dataPath, @"Falcon/Manager/Importer/Editor/Icons/trash.png", SearchOption.AllDirectories)[0];
            _trashIcon.LoadImage(File.ReadAllBytes(file));
            _manageService = new PackageManageService(infos);
            
            _moduleTab     = new ModuleTabContent(cmsService, _manageService, _packageInfos, packageGroups, _trashIcon);
            _quickStartTab = new QuickStartTabContent(cmsService, _manageService, infos);
            _gameTemplateTab = new GameTemplateTabContent(cmsService);
        }

        private string GetKeyword(FalconPackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo.Group))
                return packageInfo.Group;
             
            if (packageInfo.Name.StartsWith("falcon.helpers"))
                return "Helpers";
            if (packageInfo.Name.StartsWith("falcon.manager"))
                return "Manager";
            if (packageInfo.Name.StartsWith("falcon.tools"))
                return "Tools";
            if (packageInfo.Name.Contains("thirdparty"))
                return "Third Party";
            if (packageInfo.Name.Contains("events"))
                return "Events";
            if (packageInfo.Name.Contains("inapp") && !packageInfo.Name.Contains("update"))
                return "IAP";
            if (packageInfo.Name.Contains("packs"))
                return "Packages";
            if (packageInfo.Name.Contains("verify"))
                return "Verify";
            if (packageInfo.Name.StartsWith("falcon.modules.core"))
                return "Core";
            return "Others";
        }

        public override void Edit(EditorWindow window)
        {
            GUILayout.Space(5);

            GUIVertical(() =>
            {
                GUIHorizon(() =>
                {
                    GUILayout.Label("FALCON MODULES", EditorStyles.whiteBoldLabel);
                    
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Refresh", GUILayout.Width(100), GUILayout.Height(20)))
                    {
                        _nextController = new LoadingController();
                        var controller = _nextController as LoadingController;
                        controller.SetCMSService(_cmsService);
                        
                        RemotePackageRepository.Refresh(_cmsService);
                        PackageInfoService.Refresh();
                    }

                    GUILayout.Space(10);
                    
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_SettingsIcon"), GUILayout.Width(30)))
                    {
                        EditorApplication.ExecuteMenuItem("Falcon/Manager/Unify Module Settings");
                    }
                    
                    GUILayout.Space(5);
                    
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Download-Available"), GUILayout.Width(30)))
                    {
                        DownloadUnityPlugins();
                    }

                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow"), GUILayout.Width(30)))
                    {
                        Application.OpenURL(Configuration.DOCUMENT_URL);
                    }
                    
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_NavMeshAgent Icon"), GUILayout.Width(30), GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("Warning", "You're about to log out. Continue?", "Log out", "Cancel"))
                        {
                            AuthKeyRepository.DeleteKey();
                            _nextController = new AuthController<LoadingController>();
                        }
                    }
                });
                
                GUILayout.Space(5);
                _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
                GUILayout.Space(5);

                switch (_selectedTab)
                {
                    case 0:
                        _moduleTab.OnGUI(window);
                        break;
                    case 1:
                        _quickStartTab.OnGUI(window);
                        break;
                    case 2:
                        _gameTemplateTab.OnGUI(window);
                        break;
                }
            });
        }

        private void DownloadUnityPlugins()
        {
            var downloader = new FrameworkPluginsDownloadService();
            
            var builder    = new StringBuilder("Following Framework Plugins will be downloaded for updating purpose: ");
            builder.AppendLine();
            builder.AppendLine();

            for (var i = 0; i < downloader.GetPluginsInfo().Length; i++)
            {
                builder.AppendLine($" - {downloader.GetPluginsInfo()[i]}");
            }
            
            if (EditorUtility.DisplayDialog("Download Info", builder.ToString(), "Yes", "No"))
            {
                downloader.DownloadAndImport().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.LogError(t.Exception);
                    }
                });
            }
        }

        public override bool TryMoveNextController(out IViewController viewController)
        {
            if (_nextController != null)
            {
                viewController = _nextController;
                return true;
            }
            
            if (_moduleTab.TryGetNextController(out viewController))
                return true;
            
            if (_quickStartTab.TryGetNextController(out viewController))
                return true;

            if (_gameTemplateTab.TryGetNextController(out viewController))
                return true;

            viewController = null;
            return false;
        }
    }
}
