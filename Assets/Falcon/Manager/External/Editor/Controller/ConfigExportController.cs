/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-03
     */

namespace Falcon.Manager.External
{
	using System.IO;
	using Falcon.Manager.Shared;
	using UnityEditor;
	using UnityEngine;

	public class ConfigExportController : ACMSValidatedController
	{
		private AuthRegistryEntry     _module;
		private ConfigExporterService _configExporterService;
		
		
		public override void SetCMSService(CMSService service)
		{
			_configExporterService = new ConfigExporterService(service);
		}

		public void SetModule(AuthRegistryEntry module)
		{
			_module = module;
		}
		
		public override void Edit(EditorWindow window)
		{
			if (_module == null)
			{
				GUILayout.Label("No auth registry entry found");
				return;
			}
			
			GUILayout.Space(15);
			GUILayout.Label(_module.displayName, WindowStyles.TextAlignment(TextAnchor.MiddleCenter));
			GUILayout.Space(15);

			if (GUILayout.Button("Export To Module Locally", GUILayout.Height(30)))
			{
				var modulePath         = _module.packagePath;
				Debug.Log($"Locally exporting module: {_module.displayName} from path: {modulePath}");
				_configExporterService.ExportLocallyConfig(_module.name, modulePath);
			}

			GUILayout.Space(5);

			if (GUILayout.Button("Export to CDN", GUILayout.Height(30)))
			{
				if (!EditorUtility.DisplayDialog("Warning!!!", "This will DELETE Local SetUpFile before Export", "Export CDN", "Cancel"))
				{
					return;
				}

				var modulePath = _module.packagePath;

				var absPath       = Path.GetFullPath(modulePath);
				var localSetUpDir = _configExporterService.GetLocallyExportPath(absPath);
				if (!string.IsNullOrEmpty(localSetUpDir) && Directory.Exists(localSetUpDir))
				{
					Debug.Log("Deleting local config file...");
					var meta = $"{localSetUpDir}.meta";
					Directory.Delete(localSetUpDir, true);
					File.Delete(meta);
					AssetDatabase.Refresh();
				}

				Debug.Log($"CDN Exporting module: {_module.displayName} from path: {modulePath}");
				_configExporterService.ExportCDNConfig(_module.name, modulePath).ContinueWith(task =>
				{
					if (task.IsFaulted) Debug.LogError(task.Exception);
				});
				
			}
		}
		
		public override bool TryMoveNextController(out IViewController viewController)
		{
			viewController = null;
			return false;
		}
	}
}