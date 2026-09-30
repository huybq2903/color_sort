    /*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-28
     */


namespace Falcon.Helpers.ConfigImporter.Editor
{
	using System.Net.Http;
	using System.Threading.Tasks;
	using UnityEditor;
	using UnityEngine;

	public class RemoteCentralCoreShowingWindow : EditorWindow
	{
		private RemoteCentralCoreDownloader _downloader;

		private RemoteCentralCorePackageCollection _collection;
		
		private Vector2 _scrollPosition = Vector2.zero;
		private bool    _registryLoaded = false;
		
		[MenuItem(@"Falcon/Manager/Central Core Importer", false, 401)]
		public static void OpenWindow()
		{
			var    window = GetWindow<RemoteCentralCoreShowingWindow>("Central Core Importer");
			window.minSize = new Vector2(200, 120);
		}

		private void OnEnable()
		{
			_registryLoaded = false;
			LoadCollection().ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					Debug.LogError(t.Exception);
				}
			});
		}

		private async Task LoadCollection()
		{
			_collection = new();

			var url = $"https://jp-osa-1.linodeobjects.com/falcon-framework-libs/central-lib/central-config.json";

			var client = new HttpClient();

			using var res = await client.GetAsync(url);
			res.EnsureSuccessStatusCode();
			var content = await res.Content.ReadAsStringAsync();

			if (!string.IsNullOrEmpty(content))
			{
				_collection = JsonUtility.FromJson<RemoteCentralCorePackageCollection>(content);
			}
			
			_registryLoaded = true;
		}

		void OnGUI()
		{
			GUILayout.Space(5);
			if (!_registryLoaded)
			{
				GUILayout.Label("Configs is being loaded...");

				return;
			}
			
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
			foreach (var info in _collection.packages)
			{
				ShowOne(info);
				GUILayout.Space(5);
			}
			
			EditorGUILayout.EndScrollView();
		}

		private void ShowOne(RemoteCentralCorePackage package)
		{
			EditorGUILayout.BeginVertical("helpbox");
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical();
			GUILayout.Label(package.displayName + " v" + package.version, EditorStyles.boldLabel);

			if (!string.IsNullOrEmpty(package.date))
			{
				GUILayout.Label($" - Updated on: {package.date}", EditorStyles.miniLabel);
			}

			EditorGUILayout.EndVertical();

			if (GUILayout.Button("Install", GUILayout.Width(100), GUILayout.Height(30)))
			{
				Install(package.fileName);
				Close();
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}

		private void Install(string uri)
		{
			_downloader = new RemoteCentralCoreDownloader();
			_downloader.DownloadAndImport(uri).ContinueWith(task =>
			{
				if (task.IsFaulted)
				{
					Debug.LogError(task.Exception);
				}
			});
		}
	}
}