/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using System;
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
    public class ModuleInstallController : AViewController
    {
        private readonly CMSService _cmsService;
        private readonly AtomicRef<string> _processDescription = new("Preparing...");
        private readonly Task _task;
        private readonly CancellationTokenSource _tokenSource = new();

        public ModuleInstallController(CMSService service, PackageManageService manageService,
            FalconPackageInfo falconPackageInfo)
        {
            _cmsService = service;
            _task = RunInstallAsync(manageService, falconPackageInfo);
        }
        
        private async Task RunInstallAsync(PackageManageService manageService, FalconPackageInfo falconPackageInfo)
        {
            try
            {
                await manageService.Install(falconPackageInfo, _tokenSource.Token, _processDescription);

                // Sau khi hoàn tất, gọi dialog trên main thread
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayDialog(
                        "Install package success !!!",
                        $"Package {falconPackageInfo.DisplayName} ver {falconPackageInfo.LatestVersion} has been imported successfully",
                        "Ok"
                    );
                };
            }
            catch (OperationCanceledException)
            {
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayDialog(
                        "Installation cancelled",
                        $"Installation for {falconPackageInfo.DisplayName} was cancelled.",
                        "Ok"
                    );
                };
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayDialog(
                        "Install package failed !!!",
                        ex.Message,
                        "Ok"
                    );
                };
            }
        }

        public override void Edit(EditorWindow window)
        {
            GUIVertical(() =>
            {
                GUILayout.Space(20);

                var dots = new StringBuilder();
                for (var i = 0; i < MyTime.CurrentTimeSec % 5; i++) dots.Append("!  ");

                GUILayout.Label(
                    "Task is being process, please don't turn off this window while processing to avoid exceptions.");
                GUILayout.Space(20);
                GUILayout.Label(_processDescription.Value);

                GUILayout.Space(20);

                if (GUILayout.Button("Cancel")) _tokenSource.Cancel();
            });
        }

        public override bool TryMoveNextController(out IViewController viewController)
        {
            if (_task.IsCompleted)
            {
                viewController = new LoadingController();
                var controller = viewController as LoadingController;
                controller.SetCMSService(_cmsService);
                return true;
            }

            viewController = null;
            return false;
        }

        public override void Dispose()
        {
            _tokenSource.Dispose();
        }
    }
}