/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-23
     */


namespace Falcon.Manager.Importer
{
	using System;
	using System.Threading.Tasks;
	using Falcon.Helpers.Devkit;
	using Falcon.Manager.Shared;
	using UnityEditor;
	using UnityEngine;

	public class PackageInfoContentShowingEditor : EditorWindow
	{
		private Vector2 _scrollPosition;
		private string  _content;
		
		private static string _packageId;
		private static string _contentKind;
		
		public static void Open(string packageName, string packageId, string contentKind)
		{
			_packageId   = packageId;
			_contentKind = contentKind;

			var window = GetWindow<PackageInfoContentShowingEditor>(packageName);	
			window.minSize = new Vector2(500, 250);
			window.Load();
		}

		private void Load()
		{
			_content = null;
			if (!string.IsNullOrEmpty(_packageId))
			{
				LoadContent().ContinueWith(t =>
				{
					if (t.IsFaulted)
					{
						_content = " --- Load failed! --- ";
					}
				});
			}
		}

		private void OnGUI()
		{
			if (string.IsNullOrEmpty(_packageId))
			{
				EditorGUILayout.LabelField("Package ID is required.");
				return;
			}
			
			EditorGUILayout.LabelField(_contentKind, Styles.TextAlignment(TextAnchor.MiddleCenter));
			
			GUILayout.Space(10);
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			
			if (string.IsNullOrEmpty(_content))
			{
				EditorGUILayout.LabelField("Loading...");
			}
			else
			{
				var padding = new RectOffset(0, 0, 5, 0);
				EditorGUILayout.LabelField($"{_content}", Styles.RichText(padding, TextAnchor.MiddleLeft));
			}

			EditorGUILayout.EndScrollView();
		}

		private async Task LoadContent()
		{
			var changelogPath = $"{Configuration.kURLPrefix}/{_packageId}/{_contentKind}.md";

			var response = await new GetRequest(changelogPath).Execute();
			_content = await response.SuccessStrBody();
			if (string.IsNullOrEmpty(_content))
			{
				_content = " --- Not found! --- ";
			}
			else
			{
				_content = ConvertToRichText(_content);
			}
		}

		private string ConvertToRichText(string markdown)
		{
			return markdown
				.Replace("```csharp", "----------------------------------")
				.Replace("```", "----------------------------------");
		}
	}
}