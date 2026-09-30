/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public class FalconMenu : EditorWindow
    {
        private IViewController _controller;

        private IViewController Controller => _controller ??= new AuthController<ModuleImportCheckController>();

        private void OnDisable()
        {
            _controller?.Dispose();
        }

        private void OnGUI()
        {
            Controller.Edit(this);
            if (Event.current.type != EventType.Repaint) return;
            if (!Controller.TryMoveNextController(out var nextController)) return;
            Controller.Dispose();
            _controller = nextController;
            Repaint();
        }

        [MenuItem("Falcon/Manager/Package Importer", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<FalconMenu>("Falcon Settings", true);
            window.minSize = new Vector2(600, 900);

            window.Show();
        }


        [MenuItem("Falcon/Manager/Clear Import Queue", priority = 4)]
        public static void ClearImportQueue()
        {
            PackImportQueue.Clear();
            Debug.Log("Package import queue cleared");
        }
    }
}