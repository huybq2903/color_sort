/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-03
     */

namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System;
	using System.Threading;
	using UnityEditor;
	using UnityEngine;

	public class ConfigDownloaderWindow : EditorWindow
	{
		private static string _uri;
		
		private ConfigDownloader        _configDownloader;

		private readonly CancellationTokenSource _tokenSource = new();

		private bool _isInstalling;
		
		public static void OpenWindow(string packageId)
		{
			_uri = $"{Configuration.kURLPrefix}/{packageId}/{Configuration.SETUP_PATH}/{Configuration.CONFIG_ASSET_FILE_NAME}";
			
			var    window = GetWindow<ConfigDownloaderWindow>("Config Downloader");
			window.minSize = new Vector2(350, 80);
		}

		private void OnEnable()
		{
			Debug.Log($"Assets's uri: {_uri}");

			if (!string.IsNullOrEmpty(_uri))
			{
				_configDownloader = new ConfigDownloader();
				_isInstalling     = true;
				_configDownloader.DownloadAndImport(_uri, _tokenSource.Token).ContinueWith(task =>
				{
					_isInstalling = false;
					if (task.IsFaulted)
					{
						Debug.LogError(task.Exception);
					}
				});
			}
		}

		private void OnDisable()
		{
			_uri = string.Empty;
		}

		private void OnGUI()
		{
			GUILayout.Label($"Asset is being download, please don't turn off!");
			GUILayout.Space(20);

			if (_isInstalling)
			{
				GUILayout.Space(20);
				if (GUILayout.Button("Cancel"))
				{
					_tokenSource.Cancel();
				}
			}
		}
	}
}