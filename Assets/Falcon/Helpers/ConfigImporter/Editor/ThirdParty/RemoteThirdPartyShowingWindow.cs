/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-07
     */

namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System.Collections.Generic;
	using System.Threading;
	using UnityEditor;
	using UnityEngine;

	public class RemoteThirdPartyShowingWindow : EditorWindow
	{
		private ConfigDownloader        _configDownloader;

		private readonly CancellationTokenSource _tokenSource = new();

		private Dictionary<string, RemoteThirdPartyPackages> _infos = new();
		
		private Vector2 _scrollPosition = Vector2.zero;
		private bool    _registryLoaded = false;
		private bool    _installing     = false;
		
		[MenuItem(@"Falcon/Manager/Third-Party Importer", false, 401)]
		public static void OpenWindow()
		{
			var    window = GetWindow<RemoteThirdPartyShowingWindow>("Third Party");
			window.minSize = new Vector2(450, 100);
		}

		void OnGUI()
		{
			if (!_registryLoaded)
			{
				GUILayout.Label("Third Party lib configs is being loaded...");
				if (RemoteThirdPartyInfoService.TryGetInfos(out var infos))
				{
					_infos          = infos;
					_registryLoaded = true;
				}

				return;
			}

			if (_installing)
			{
				GUILayout.Label($"Asset is being download, please don't turn off!");
				GUILayout.Space(20);
				
				return;
			}
			
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
			foreach (var info in _infos)
			{
				ShowOne(info);
				GUILayout.Space(5);
			}
			
			EditorGUILayout.EndScrollView();
		}

		private void ShowOne(KeyValuePair<string, RemoteThirdPartyPackages> info)
		{
			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(info.Key, EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(EditorGUIUtility.IconContent("d_Linked@2x"), GUILayout.Width(30), GUILayout.Height(20)))
			{
				Application.OpenURL(info.Value.url);
			}
			
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.BeginVertical("helpbox");
			foreach (var package in info.Value.packages)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label(package.displayName + " v" + package.version);
				
				if (GUILayout.Button("Install", GUILayout.Width(100), GUILayout.Height(20)))
				{
					var uri = $"{Configuration.k3rdURLPrefix}/{info.Key}/{package.fileName}";
					Install(uri);
				}
				
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndVertical();
			
			EditorGUILayout.EndVertical();
		}

		private void Install(string uri)
		{
			_installing = true;
			_configDownloader = new ConfigDownloader();
			_configDownloader.DownloadAndImport(uri, _tokenSource.Token).ContinueWith(task =>
			{
				_installing = false;
				if (task.IsFaulted)
				{
					Debug.LogError(task.Exception);
				}
			});
		}
	}
}