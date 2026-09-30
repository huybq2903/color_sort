//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(EdgeLightingFilter), true)]
	[CanEditMultipleObjects]
	internal class EdgeLightingFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Edge Lighting Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/components/filters/drop-shadow-filter/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Review <b>UIFX - Pro Bundle</b>", AssetStoreProBundleReviewUrl),
								new AboutButton("Review <b>UIFX - Indie Bundle</b>", AssetStoreIndieBundleReviewUrl),
								new AboutButton("Review <b>UIFX - Starter Bundle</b>", AssetStoreStarterBundleReviewUrl),
							}
						},
						new AboutSection("UIFX Support")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Discord Community", DiscordUrl),
								new AboutButton("Post to Unity Discussions", "https://discussions.unity.com/t/released-uifx-drop-shadow-filter/940748"),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeFromStarterBundle, s_upgradeFromIndieBundle, s_aboutInfo } );

		private static readonly GUIContent Content_Light = new GUIContent("Light");
		private static readonly GUIContent Content_Blur = new GUIContent("Blur");
		private static readonly GUIContent Content_Shadow = new GUIContent("Shadow");
		private static readonly GUIContent Content_Enabled = new GUIContent("Enabled");
		private static readonly GUIContent Content_Distance = new GUIContent("Distance");

		private SerializedProperty _propBlur;
		private SerializedProperty _propAngle;
		private SerializedProperty _propDistance;
		private SerializedProperty _propLightColor;
		private SerializedProperty _propShadowColor;

		private SerializedProperty _propShadowEnabled;
		private SerializedProperty _propShadowBlur;
		private SerializedProperty _propShadowDistance;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;
		private SerializedProperty _propExpand;

		protected override void OnEnable()
		{
			_propBlur = VerifyFindProperty("_blur");
			_propAngle = VerifyFindProperty("_angle");
			_propDistance = VerifyFindProperty("_distance");
			_propLightColor = VerifyFindProperty("_lightColor");
			_propShadowEnabled = VerifyFindProperty("_shadowEnabled");
			_propShadowBlur = VerifyFindProperty("_shadowBlur");
			_propShadowDistance = VerifyFindProperty("_shadowDistance");
			_propShadowColor = VerifyFindProperty("_shadowColor");
			_propStrength = VerifyFindProperty("_strength");
			_propRenderSpace = VerifyFindProperty("_renderSpace");
			_propExpand = VerifyFindProperty("_expand");
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			var filter = this.target as FilterBase;

			if (OnInspectorGUI_Check(filter))
			{
				return;
			}

			GUILayout.Label(Content_Light, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propAngle);
			EditorGUILayout.PropertyField(_propDistance);
			EditorGUILayout.PropertyField(_propBlur);
			EditorGUILayout.PropertyField(_propLightColor, Content_Color);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Shadow, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propShadowEnabled, Content_Enabled);
			if (_propShadowEnabled.boolValue)
			{
				EditorGUILayout.PropertyField(_propShadowDistance, Content_Distance);
				EditorGUILayout.PropertyField(_propShadowBlur, Content_Blur);
				EditorGUILayout.PropertyField(_propShadowColor, Content_Color);
			}
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EnumAsToolbarCompact(_propRenderSpace);
			EnumAsToolbarCompact(_propExpand);
			DrawStrengthProperty(_propStrength);
			EditorGUI.indentLevel--;

			if (OnInspectorGUI_Baking(filter))
			{
				return;
			}

			FilterBaseEditor.OnInspectorGUI_Debug(filter);

			serializedObject.ApplyModifiedProperties();
		}
	}
}