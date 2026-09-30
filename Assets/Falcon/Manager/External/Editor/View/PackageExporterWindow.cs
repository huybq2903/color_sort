/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.External
{
    public class PackageExporterWindow : EditorWindow
    {
        private IViewController _controller;

        private IViewController Controller => _controller ??= new AuthController<PackageExportController>();

        private void OnDisable()
        {
            _controller?.Dispose();
        }

        private void OnGUI()
        {
            Controller.Edit(this);
            if(Event.current.type != EventType.Repaint) return;
            if (!Controller.TryMoveNextController(out var nextController)) return;
            Controller.Dispose();
            _controller = nextController;
            Repaint();
        }


        [MenuItem(@"Falcon/Manager/Package Exporter", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<PackageExporterWindow>("Package Exporter");
            window.minSize = new Vector2(450, 300);
            LocalPackageRepository.Refresh();
        }
    }
}