//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(CompositeLayerChild), true)]
	[CanEditMultipleObjects]
	internal class CompositeLayerChildEditor : BaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Composite Layer Child\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/components/sources/composite-layer/"),
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

		private SerializedProperty _propCompositeLayer;

		void OnEnable()
		{
			_propCompositeLayer = VerifyFindProperty("_compositeParentGo");
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			GUI.enabled = false;
			EditorGUILayout.PropertyField(_propCompositeLayer);
			GUI.enabled = true;

			foreach (CompositeLayerChild compositeLayerChild in this.targets)
			{
				if (!CompositeLayerChild.IsLastUIComponentOnGameObject(compositeLayerChild))
				{
					EditorGUILayout.HelpBox(string.Format("This component must be the last UI component on the GameObject '{0}', otherwise incorrect rendering will occur. Please change component ordering to fix this.", compositeLayerChild.gameObject.name), MessageType.Error, true);
					break;
				}
			}
		}
	}
}