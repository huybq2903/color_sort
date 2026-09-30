/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-10-16
     */


namespace Falcon.Manager.Importer
{
	using UnityEditor;
	using UnityEngine;

	public class ContentShowingEditor : EditorWindow
	{
		private Vector2 _scrollPosition;
		private string  _header;
		private string  _content;
		
		public static void Open(string title, string header, string content)
		{
			var window = GetWindow<ContentShowingEditor>(title);	
			window.titleContent = new GUIContent(title);
			window.minSize = new Vector2(400, 200);
			window.ShowContent(header, content);
		}

		private void ShowContent(string header, string content)
		{
			_header  = header;
			_content = content;
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField(_header, Styles.TextAlignment(TextAnchor.MiddleCenter));
			
			GUILayout.Space(10);
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			
			var padding = new RectOffset(0, 0, 5, 0);
			EditorGUILayout.LabelField($"{_content}", Styles.RichText(padding, TextAnchor.MiddleLeft));
			

			EditorGUILayout.EndScrollView();
		}
	}
}