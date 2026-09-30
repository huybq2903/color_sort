/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-26
     */


namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System.Collections.Generic;
	using System.IO;
	using Unity.Plastic.Newtonsoft.Json;
	using UnityEditor;
	using UnityEngine;
	using Application = UnityEngine.Application;

	[InitializeOnLoad]
	public static class ManifestPackageManageService
	{
		private static readonly string kManifestPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), "Packages", "manifest.json");

		private static readonly Dictionary<string, string> kRequiredPackages = new()
		{
			{ "com.unity.addressables", "2.5.0" },
			{ "com.unity.nuget.newtonsoft-json", "3.2.1" },
			{ "com.unity.purchasing", "5.3.0" },
			{ "com.cysharp.unitask", "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask" },
			{ "com.annulusgames.lit-motion", "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion" },
			{ "com.coffee.ui-particle", "https://github.com/mob-sakai/ParticleEffectForUGUI.git" }
		};

		static ManifestPackageManageService()
		{
			AssetDatabase.importPackageCompleted -= OnImportComplete;
			AssetDatabase.importPackageCompleted += OnImportComplete;
		}

		private static void OnImportComplete(string packageName)
		{
			if (!File.Exists(kManifestPath))
			{
				Debug.LogWarning($"[ManifestPackageManageService] manifest.json not found: {kManifestPath}");
				return;
			}

			var json     = File.ReadAllText(kManifestPath);
			var manifest = JsonConvert.DeserializeObject<ManifestModel>(json);

			if (manifest == null)
			{
				Debug.LogWarning("[ManifestPackageManageService] Failed to parse manifest.json");
				return;
			}

			var dirty = false;

			foreach (var kvp in kRequiredPackages)
			{
				if (manifest.dependencies.ContainsKey(kvp.Key)) continue;

				manifest.dependencies[kvp.Key] = kvp.Value;
				dirty = true;
			}

			if (!dirty) return;

			var output = JsonConvert.SerializeObject(manifest, Formatting.Indented);
			File.WriteAllText(kManifestPath, output);
			Debug.Log("[ManifestPackageManageService] Added required packages to manifest.json");

			if (EditorUtility.DisplayDialog(
				    "Manifest Updated",
				    "Required packages were added to manifest.json.\nRestart Unity to apply changes?",
				    "Restart", "Later"))
			{
				EditorApplication.OpenProject(System.Environment.CurrentDirectory);
			}
		}
	}
}
