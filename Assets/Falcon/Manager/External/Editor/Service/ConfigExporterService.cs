/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-02
     */

using UnityEngine;

namespace Falcon.Manager.External
{
	using System.IO;
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Threading.Tasks;
	using Falcon.Manager.Shared;
	using Newtonsoft.Json;
	using UnityEditor;

	/// <summary>
	/// Export external module's config assets into module itself to install later on by user
	/// </summary>
	public class ConfigExporterService
	{
		private readonly CMSService _cmsService;
        
		internal ConfigExporterService(CMSService cmsService)
		{
			_cmsService = cmsService;
		}
		
		internal static bool IsExistConfigs(string modulePath)
		{
			var pathToExport = Path.Combine(Application.dataPath, Configuration.PATH_PREFIX_TO_EXPORT);

			if (Directory.Exists(pathToExport))
			{
				var exportPath = modulePath.Replace(@"\Falcon\", @"\FalconAssets\");
				return Directory.Exists(exportPath);
			}

			return false;
		}
		
		internal async Task ExportCDNConfig(string module, string modulePath)
		{
			var configPath = GetModuleConfigPath(modulePath);
			var absPath    = Path.GetFullPath(configPath);
			if (Directory.Exists(absPath))
			{
				var exportPath = GetTempExportPath(module);
				if (!string.IsNullOrEmpty(exportPath))
				{
					var assetExportPath = $"{exportPath}/{Configuration.CONFIG_ASSET_FILE_NAME}";
					Debug.Log($"Export {configPath} to {assetExportPath}");
					AssetDatabase.ExportPackage(
						configPath,
						assetExportPath,
						ExportPackageOptions.Recurse);

					AssetDatabase.Refresh();
					Debug.Log($"Config for {module} exported at {assetExportPath}");
					
					var multipart = new MultipartFormDataContent();
					
					await using var fileStream  = File.OpenRead(assetExportPath);
                
					var fileName    = Path.GetFileName(assetExportPath);
					var fileContent = new StreamContent(fileStream);
					fileContent.Headers.ContentType = new MediaTypeHeaderValue(@"application/zip");
                
					multipart.Add(fileContent, "file", fileName);
					
					bool uploaded = await _cmsService.UploadConfig(module, new MultipartFileContent(multipart));
					
					if (uploaded)
					{
						EditorUtility.DisplayDialog("Success", "Uploaded " + module + " configs", "OK");
					}
					else
					{
						EditorUtility.DisplayDialog("Upload Failed", "The Config upload failed.", "OK");
					}
					
					fileStream.Close();
					FileUtil.DeleteFileOrDirectory(assetExportPath);
				}
			}
		}

		internal void ExportLocallyConfig(string module, string modulePath)
		{
			var configPath = GetModuleConfigPath(modulePath);
			var absPath = Path.GetFullPath(configPath);
			if (Directory.Exists(absPath))
			{
				var exportPath = GetLocallyExportPath(modulePath);
				if (!string.IsNullOrEmpty(exportPath))
				{
					var assetExportPath = $"{exportPath}/{Configuration.CONFIG_ASSET_FILE_NAME}";
					Debug.Log($"Export {configPath} to {assetExportPath}");
					AssetDatabase.ExportPackage(
						configPath,
						assetExportPath,
						ExportPackageOptions.Recurse);
				    
					AssetDatabase.Refresh();
					Debug.Log($"Config for {module} exported at {assetExportPath}");
				    
					EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>($"{modulePath}/{Configuration.SETUP_PATH}/{Configuration.CONFIG_ASSET_FILE_NAME}"));
				}
			}
		}

		private string GetModuleConfigPath(string modulePath)
		{
			return modulePath.Replace(@"\Falcon\", @"\FalconAssets\");
		}

		private string GetTempExportPath(string module)
		{
			var exportPath = Application.dataPath.Replace("Assets", $"Temp/{module}");
			if (!Directory.Exists(exportPath))
			{
				Directory.CreateDirectory(exportPath);
			}

			return exportPath;
		}
		
		internal string GetLocallyExportPath(string modulePath)
		{
			var exportPath = $"{modulePath}/{Configuration.SETUP_PATH}";
			if (!Directory.Exists(exportPath))
			{
				Directory.CreateDirectory(exportPath);
			}

			return exportPath;
		}
	}
}
