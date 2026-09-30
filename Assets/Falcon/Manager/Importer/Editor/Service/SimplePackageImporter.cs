/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-03-03
     */


namespace Falcon.Manager.Importer
{
	using System;
	using System.Net;
	using System.Threading.Tasks;
	using Falcon.Helpers.Devkit;
	using UnityEditor;
	using UnityEngine;

	internal static class SimplePackageImporter
	{
		internal static async Task DownloadAndImport(string title, string url, IPackageImportService importService = null)
		{
			if (importService == null)
			{
				importService = new DefaultUnityImporter();
			}
			
			var       fTempFile = new FLocalFileRepository().GetTempFile(GUID.Generate().ToString().Replace("-", "") + ".unitypackage");
			using var client    = new WebClient();
			client.Proxy = null;        // avoid auto proxy may cause slow speed
            
			EditorUtility.DisplayProgressBar(title, "Downloading... 0%", 0f);
			client.DownloadProgressChanged += (_, e) =>
			{
				EditorUtility.DisplayProgressBar(title, $"Downloading... {e.ProgressPercentage}%", e.ProgressPercentage / 100f);
			};
			client.DownloadFileCompleted += (_, _) =>
			{
				Debug.Log($"{title} Download Completed!");
			};
            
			await client.DownloadFileTaskAsync(new Uri(url), fTempFile.FilePath);
            
			EditorUtility.ClearProgressBar();
			importService.ImportPackage(fTempFile.FilePath, true);
			fTempFile.Delete();
		}
	}
}