    /*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-28
     */



namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System;
	using System.IO;
	using System.Net;
	using System.Threading;
	using System.Threading.Tasks;
	using UnityEditor;
	using UnityEngine;

	public class RemoteCentralCoreDownloader
	{
		public RemoteCentralCoreDownloader()
		{
			
		}
		
		public async Task DownloadAndImport(string url)
		{
			var       filePath = GetTempFile(GUID.Generate().ToString().Replace("-", "") + ".unitypackage");
			using var client   = new WebClient();
			client.Proxy = null;        // avoid auto proxy may cause slow speed
            
			EditorUtility.DisplayProgressBar("Central Core", "Downloading... 0%", 0f);
			client.DownloadProgressChanged += (_, e) =>
			{
				EditorUtility.DisplayProgressBar("Central Core", $"Downloading... {e.ProgressPercentage}%", e.ProgressPercentage / 100f);
			};
			client.DownloadFileCompleted += (_, _) =>
			{
				Debug.Log($"Central Core Download Completed");
			};
            
			await client.DownloadFileTaskAsync(new Uri(url), filePath);
			
			EditorUtility.ClearProgressBar();
            
			AssetDatabase.ImportPackage(filePath, true);
			File.Delete(filePath);
		}
		
		private string GetTempFile(string fileName)
		{
			string folder = Application.temporaryCachePath;
			if (string.IsNullOrEmpty(folder))
			{
				folder = Application.persistentDataPath;
			}
			
			if(!fileName.StartsWith(folder)) fileName = Path.Combine(folder , fileName);
			return fileName;
		}
	}
}