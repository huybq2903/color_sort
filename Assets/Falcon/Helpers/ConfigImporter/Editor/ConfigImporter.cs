/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
 */


namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using Falcon.Helpers.Addressable;
	using Newtonsoft.Json;
	using UnityEditor;
	using UnityEngine;

	/// <summary>
	/// API for import module's config assets to client
	/// </summary>
	[InitializeOnLoad]
	public static class ConfigImporter
	{
		private const string CONFIG_PATH_SAVE = "CONFIG_PATH_SAVE";

		private static Action _onImportComplete;

		static ConfigImporter()
		{
			AssetDatabase.importPackageCancelled -= OnImportCancelled;
			AssetDatabase.importPackageCancelled += OnImportCancelled;
			
			AssetDatabase.importPackageFailed -= OnImportFailed;
			AssetDatabase.importPackageFailed += OnImportFailed;
			
			AssetDatabase.importPackageCompleted -= OnImportComplete;
			AssetDatabase.importPackageCompleted += OnImportComplete;
		}

		private static void OnImportCancelled(string packageName)
		{
			_onImportComplete = null;
			EditorPrefs.SetString(CONFIG_PATH_SAVE, string.Empty);
		}

		private static void OnImportFailed(string packageName, string error)
		{
			_onImportComplete = null;
			EditorPrefs.SetString(CONFIG_PATH_SAVE, string.Empty);
		}

		/// <summary>
		/// This will import and mark addressable of valid pattern files.
		/// </summary>
		/// <param name="modulePath">Related Module Path, for e.g. @"Falcon/Modules/Level"</param>
		/// <param name="onImportComplete">Callback when import is completed</param>
		public static void ImportConfig(string modulePath, System.Action onImportComplete = null)
		{
			if (string.IsNullOrEmpty(modulePath))
			{
				_onImportComplete = null;
				EditorUtility.DisplayDialog("Error", "Path is Null or Empty", "Ok");
				return;
			}

			var path = modulePath;
			if (!modulePath.StartsWith("Assets"))
			{
				path = Path.Combine("Assets", modulePath);
			}
			
			_onImportComplete = onImportComplete;

			var tempPath = Path.Combine(path, Configuration.SETUP_PATH, Configuration.CONFIG_ASSET_FILE_NAME);
			if (File.Exists(tempPath))
			{
				Debug.Log("Exist locally");
				ImportLocally(tempPath);
			}
			else
			{
				Debug.Log("Not Exist locally. Download from CDN");
				ImportViaCDN(path);
			}
		}

		private static void ImportLocally(string path)
		{
			var savedPath = Path.GetDirectoryName(path).Replace(Configuration.SETUP_PATH, "");
			EditorPrefs.SetString(CONFIG_PATH_SAVE, savedPath);

			AssetDatabase.ImportPackage(path, true);
		}

		private static void ImportViaCDN(string path)
		{
			path += @"/";
			
			var savedPath = Path.GetDirectoryName(path);
			EditorPrefs.SetString(CONFIG_PATH_SAVE, savedPath);
			
			var dir              = new DirectoryInfo(Application.dataPath.Replace("Assets", path));
			var packageJsonFiles = dir.GetFiles("package.json", SearchOption.AllDirectories);
			if (packageJsonFiles.Length > 0)
			{
				string jsonContent = File.ReadAllText(packageJsonFiles[0].FullName);
				var    packageData = JsonConvert.DeserializeObject<SimplePackage>(jsonContent);
				
				if (packageData != null)
					ConfigDownloaderWindow.OpenWindow(packageData.name);
			}
		}

		private static void OnImportComplete(string packageName)
		{
			var assetImportedPath = GetAssetImportedPath();
			var builder           = new StringBuilder();

			if (!string.IsNullOrEmpty(assetImportedPath))
			{
				Debug.Log($"--> Addressable checking at: {assetImportedPath}");
				var absPath = Path.GetFullPath(assetImportedPath);
				var dir     = new DirectoryInfo(absPath);

				var addressables = dir.GetDirectories("Addressable", SearchOption.AllDirectories);
				var toAdd        = new List<UnityEngine.Object>(); // gom rồi MakeAssetsAddressable 1 lần (batch)

				foreach (var address in addressables)
				{
					var addressInfo = new DirectoryInfo(address.FullName);
					var files       = addressInfo.GetFiles("*.prefab", SearchOption.AllDirectories);

					foreach (var file in files)
					{
						var split = file.FullName.Split(new[] { @"\Assets\" }, StringSplitOptions.RemoveEmptyEntries);
						if (split.Length > 1)
						{
							var assetPath = Path.Combine("Assets", split[1]);

							var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
							if (asset != null && !AddressableHelper.IsInAddressables(asset))
							{
								toAdd.Add(asset);
								builder.Append($"- {asset.name}").AppendLine();

								Debug.Log($"{asset.name} was added to 'Default Addressable Group'");
							}
						}
					}
				}

				if (toAdd.Count > 0)
					AddressableHelper.MakeAssetsAddressable(toAdd, "Packed Assets", true); // batch: scan map + SaveAssets 1 lần

				if (builder.Length > 0)
				{
					builder.AppendLine();
					builder.Append("These Addressable Files have been imported successfully!");
					EditorUtility.DisplayDialog("Asset Imported", builder.ToString(), "Ok");
				}
				else
				{
					EditorUtility.DisplayDialog("Asset Imported", "Asset Imported", "Ok");
				}
				
				_onImportComplete?.Invoke();
			}

			_onImportComplete = null;
			EditorPrefs.SetString(CONFIG_PATH_SAVE, string.Empty);
		}

		private static string GetAssetImportedPath()
		{
			var path       = EditorPrefs.GetString(CONFIG_PATH_SAVE, string.Empty);
			var exportPath = path.Replace(@"\Falcon\", @"\FalconAssets\");

			if (Directory.Exists(exportPath))
			{
				return exportPath;
			}

			return string.Empty;
		}
		
		[Serializable]
		public class SimplePackage
		{
			public string name;
			public string displayName;
		}
	}
}