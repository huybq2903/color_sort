/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using System.Text;
using Falcon.Helpers.Devkit;
using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public class LoadingController : ACMSValidatedController
    {
        private CMSService _cmsService;
        
        public override void SetCMSService(CMSService service)
        {
            _cmsService = service;
        }
        
        public override void Edit(EditorWindow window)
        {
            GUIVertical(() =>
            {
                GUILayout.Space(20);

                var dots = new StringBuilder();
                for (var i = 0; i < MyTime.CurrentTimeSec % 5; i++) dots.Append("!  ");

                GUILayout.Label($"Modules are being Loaded. please wait{dots}");
            });
        }

        public override bool TryMoveNextController(out IViewController viewController)
        {
            if (PackageInfoService.TryGetInfos(_cmsService, out var infos))
            {
                viewController = new ModulesShowingController(_cmsService, infos);
                return true;
            }

            viewController = null;
            return false;
        }
    }
}