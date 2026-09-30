/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    using System;
    using System.Linq;

    public class ModuleAskingController : AViewController
    {
        private readonly CMSService                      _cmsService;
        private readonly FalconPackageInfo               _falconPackageInfo;
        private readonly PackageManageService            _manageService;
        private readonly AtomicRef<string>               _processDescription = new("Resolving...");
        private readonly Task<ICollection<IPackageInfo>> _task;
        private readonly CancellationTokenSource         _tokenSource = new();

        public ModuleAskingController(CMSService cmsService, PackageManageService manageService, FalconPackageInfo falconPackageInfo)
        {
            _cmsService        = cmsService;
            _manageService     = manageService;
            _falconPackageInfo = falconPackageInfo;
            _task              = manageService.GetRequiredDependencies(falconPackageInfo, _tokenSource.Token, _processDescription);
        }

        public override void Edit(EditorWindow window)
        {
            GUIVertical(() =>
            {
                GUILayout.Space(20);

                var dots = new StringBuilder();
                for (var i = 0; i < MyTime.CurrentTimeSec % 5; i++) dots.Append("!  ");

                GUILayout.Label(
                    "Dependencies is being resolved, please don't turn off this window while processing to avoid exceptions.");
                GUILayout.Space(20);
                GUILayout.Label(_processDescription.Value);

                GUILayout.Space(20);

                if (GUILayout.Button("Cancel")) _tokenSource.Cancel();
            });
        }

        public override bool TryMoveNextController(out IViewController viewController)
        {
            if (!_task.IsCompleted)
            {
                viewController = null;
                return false;
            }

            if (_task.IsFaulted)
            {
                Debug.LogError(_task.Exception);
                viewController = new LoadingController();
                return true;
            }
            
#if UNITY_EDITOR_OSX
            viewController = new ModuleInstallController(_cmsService, _manageService, _falconPackageInfo);
            return true;
#endif

            var requiredDependencies = _task.Result.ToList();
            requiredDependencies.Remove(_falconPackageInfo);    // remove itself
            
            if (requiredDependencies.Count == 0)
            {
                viewController = new ModuleInstallController(_cmsService, _manageService, _falconPackageInfo);
            }
            else
            {
                var requireModuleString = new StringBuilder();
                requireModuleString
                    .Append("The following modules are required for" + _falconPackageInfo.DisplayName + ":")
                    .AppendLine();
                foreach (var info in requiredDependencies)
                {
                    requireModuleString.Append("  - ").Append(info.DisplayName).AppendLine();
                }

                requireModuleString.Append("All of this will be auto installed/updated.");

                if (EditorUtility.DisplayDialog("Additional module require!!!", requireModuleString.ToString(), "Ok",
                        "Cancel"))
                    viewController = new ModuleInstallController(_cmsService, _manageService, _falconPackageInfo);
                else
                {
                    viewController = new LoadingController();
                    var controller = viewController as LoadingController;
                    controller.SetCMSService(_cmsService);
                }
            }

            return true;
        }

        public override void Dispose()
        {
            _tokenSource.Dispose();
        }
    }
}