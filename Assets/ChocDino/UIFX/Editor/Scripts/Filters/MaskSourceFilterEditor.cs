//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(MaskSourceFilter), true)]
	[CanEditMultipleObjects]
	internal class MaskSourceFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Mask Source Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/components/filters/mask-source-filter/"),
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
								new AboutButton("Post to Unity Discussions", ForumBundleUrl),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeFromStarterBundle, s_upgradeFromIndieBundle, s_aboutInfo } );

		private SerializedProperty _propShowMaskGraphic;
		private SerializedProperty _propInvertMask;
		private SerializedProperty _propStrength;

		protected override void OnEnable()
		{
			_propShowMaskGraphic = VerifyFindProperty("_showMaskGraphic");
			_propInvertMask = VerifyFindProperty("_invertMask");
			_propStrength = VerifyFindProperty("_strength");
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			EditorGUILayout.PropertyField(_propShowMaskGraphic);
			EditorGUILayout.PropertyField(_propInvertMask);
			DrawStrengthProperty(_propStrength);

			serializedObject.ApplyModifiedProperties();
		}
	}
}