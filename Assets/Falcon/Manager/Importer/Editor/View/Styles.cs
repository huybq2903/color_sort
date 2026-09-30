/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-20
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
	using UnityEngine;

	internal static class Styles
	{
		private static GUIStyle _textAlignment;
		public static GUIStyle TextAlignment(TextAnchor anchor)
		{
			_textAlignment           ??= new GUIStyle(GUI.skin.label);
			_textAlignment.alignment =   anchor;
			return _textAlignment;
		}

		private static GUIStyle _richText;
		public static GUIStyle RichText(RectOffset padding, TextAnchor anchor)
		{
			_richText           ??= new GUIStyle(GUI.skin.label);
			_richText.richText  =   true;
			_richText.wordWrap  =   true;
			_richText.alignment =   anchor;
			_richText.padding   =   padding;
			return _richText;
		}
	}
}