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
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public class QuickStartInstallController : AViewController
    {
        private readonly CMSService _cmsService;
        private readonly AtomicRef<string> _processDescription = new("Preparing...");
        private readonly Task _task;
        private readonly CancellationTokenSource _tokenSource = new();
        private readonly int _moduleCount;

        public QuickStartInstallController(CMSService service, PackageManageService manageService,
            List<FalconPackageInfo> modules)
        {
            _cmsService  = service;
            _moduleCount = modules.Count;
            _task        = RunBatchInstallAsync(manageService, modules);
        }

        private async Task RunBatchInstallAsync(PackageManageService manageService, List<FalconPackageInfo> modules)
        {
            try
            {
                await manageService.BatchInstall(modules, _tokenSource.Token, _processDescription);

                var names = string.Join(", ", modules.Select(m => m.DisplayName));
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.DisplayDialog(
                        "Quick Start Complete",
                        $"{modules.Count} modules have been installed successfully:\n{names}",
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
                        "Quick Start installation was cancelled.",
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
                        "Install failed",
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

                GUILayout.Label($"Quick Start: Installing {_moduleCount} modules{dots}");
                GUILayout.Label("Please don't close this window while processing to avoid exceptions.");
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
