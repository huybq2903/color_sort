/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-02
     */


namespace Falcon.Manager.External
{
	using Falcon.Manager.Shared;
	using UnityEditor;
	using UnityEngine;

	public class ConfigExporterWindow : EditorWindow
	{
		private IViewController _controller;

		private IViewController Controller => _controller ??= new AuthController<ConfigExportController>();

		private static  AuthRegistryEntry _package;
		
		private void OnDisable()
		{
			_package = null;
			_controller?.Dispose();
		}

		private void OnGUI()
		{
			Controller.Edit(this);
			if(Event.current.type != EventType.Repaint) return;
			if (!Controller.TryMoveNextController(out var nextController)) return;
			Controller.Dispose();
			_controller = nextController;
			if (_controller is ConfigExportController configExportController)
			{
				configExportController.SetModule(_package);
			}
			Repaint();
		}
		
		public static void ShowWindow(AuthRegistryEntry package)
		{
			if (_package != null || package == null) return;
			
			_package = package;
			var window = GetWindow<ConfigExporterWindow>("Config Exporter");
			window.minSize = new Vector2(300, 120);
		}
	}
}