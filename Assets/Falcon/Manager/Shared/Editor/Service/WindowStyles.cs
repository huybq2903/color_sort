/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-20
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Shared
{
	using UnityEngine;

	public static class WindowStyles
	{
		public static readonly Color kRed    = new(1f, 0.392f, 0.392f);
		public static readonly Color kYellow = new(1f, 0.882f, 0.384f);
		public static readonly Color kGreen  = new(0.1843f, 0.8666f, 0.5725f);
		public static readonly Color kBlue   = new(0.3294f, 0.549f, 1f);
		
		private static GUIStyle _textColor;
		public static GUIStyle TextColor(Color c)
		{
			_textColor ??= new GUIStyle(GUI.skin.label);

			_textColor.normal.textColor = c;
			return _textColor;
		}

		private static GUIStyle _textBoldColor;
		public static GUIStyle TextBoldColor(Color c)
		{
			_textBoldColor ??= new GUIStyle(GUI.skin.label);

			_textBoldColor.normal.textColor = c;
			_textBoldColor.fontStyle        = FontStyle.Bold;
			return _textBoldColor;
		}
		
		private static GUIStyle _textMiniColor;
		public static GUIStyle TextMiniColor(Color c)
		{
			_textMiniColor ??= new GUIStyle(GUI.skin.label);

			_textMiniColor.normal.textColor = c;
			_textMiniColor.fontSize         = 10;
			return _textMiniColor;
		}
		
		private static GUIStyle _textAlignment;
		public static GUIStyle TextAlignment(TextAnchor anchor)
		{
			_textAlignment           ??= new GUIStyle(GUI.skin.label);
			_textAlignment.alignment =   anchor;
			return _textAlignment;
		}
	}
}