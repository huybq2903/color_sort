/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-03
     */


namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System;
	using System.Net;
	using System.Threading;
	using System.Threading.Tasks;
	using Falcon.Helpers.Devkit;
	using UnityEditor;
	using UnityEngine;

	public class ConfigDownloader
	{
		private readonly FLocalFileRepository _fileRepository;

		public ConfigDownloader()
		{
			_fileRepository = new FLocalFileRepository();
		}
		
		public async Task DownloadAndImport(string url, CancellationToken token)
		{
			var       fTempFile = _fileRepository.GetTempFile(GUID.Generate().ToString().Replace("-", "") + ".unitypackage");
			using var client    = new WebClient();
			client.Proxy = null;        // avoid auto proxy may cause slow speed
            
			EditorUtility.DisplayProgressBar("Config", "Downloading... 0%", 0f);
			client.DownloadProgressChanged += (_, e) =>
			{
				EditorUtility.DisplayProgressBar("Config", $"Downloading... {e.ProgressPercentage}%", e.ProgressPercentage / 100f);
			};
			client.DownloadFileCompleted += (_, _) =>
			{
				Debug.Log($"Config Download Completed");
			};
            
			await client.DownloadFileTaskAsync(new Uri(url), fTempFile.FilePath);
			token.ThrowIfCancellationRequested();
            
			EditorUtility.ClearProgressBar();
			AssetDatabase.ImportPackage(fTempFile.FilePath, true);
			fTempFile.Delete();
		}
	}
}