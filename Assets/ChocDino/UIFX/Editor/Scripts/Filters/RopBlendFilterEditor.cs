//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(ROPBlendFilter), true)]
	[CanEditMultipleObjects]
	internal class ROPBlendFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - ROP Blend Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/components/filters/fill-gradient-filter/"),
							}
						},
						new AboutSection("Unity Asset Store Review\r\n<color=#ffd700>★★★★☆</color>")
						{
							buttons = new AboutButton[]
							{
								//new AboutButton("Review <b>UIFX - Fill Filter</b>", "https://assetstore.unity.com/packages/slug/274847?aid=1100lSvNe#reviews"),
								new AboutButton("Review <b>UIFX Bundle</b>", "https://assetstore.unity.com/packages/slug/266945?aid=1100lSvNe#reviews"),
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

		private SerializedProperty _propColor;
		private SerializedProperty _propBlendMode;
		private SerializedProperty _propCustomOp;
		private SerializedProperty _propCustomSrc;
		private SerializedProperty _propCustomDst;
		private SerializedProperty _propStrength;

		protected override void OnEnable()
		{
			_propColor = VerifyFindProperty("_color");
			_propBlendMode = VerifyFindProperty("_blendMode");
			_propCustomOp = VerifyFindProperty("_customOp");
			_propCustomSrc = VerifyFindProperty("_customSrc");
			_propCustomDst = VerifyFindProperty("_customDst");
			_propStrength = VerifyFindProperty("_strength");
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			var filter = this.target as FilterBase;
			var blendFilter = this.target as ROPBlendFilter;

			if (OnInspectorGUI_Check(filter))
			{
				return;
			}

			{
				EditorGUILayout.PropertyField(_propColor);
				EnumWithButtons(_propBlendMode);
				if (_propBlendMode.intValue == (int)ROPBlendMode.Custom)
				{
					EditorGUI.indentLevel++;
					EnumWithButtons(_propCustomOp);
					if (_propCustomOp.intValue != (int)UnityEngine.Rendering.BlendOp.Min &&
						_propCustomOp.intValue != (int)UnityEngine.Rendering.BlendOp.Max)
					{
						EnumWithButtons(_propCustomSrc);
						EnumWithButtons(_propCustomDst);
					}
					EditorGUI.indentLevel--;
				}
				else
				{
					bool requiresHdr = ROPBlendFilter.IsModeRequiresHdr((ROPBlendMode)_propBlendMode.intValue);
					if (requiresHdr)
					{
						bool hasHdr = blendFilter.IsHdrRenderingSupported();
						if (!hasHdr)
						{
							EditorGUILayout.HelpBox("This blend mode requires an HDR backbuffer but none was detected.  If you can't see the blend result then make sure HDR is enabled and that the Canvas renderMode is not set to ScreenSpaceOverlay.", MessageType.Warning, true);
							EditorGUILayout.Space();
						}
					}
				}
			}

			{
				GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				DrawStrengthProperty(_propStrength);
				EditorGUI.indentLevel--;
			}

			if (OnInspectorGUI_Baking(filter))
			{
				return;
			}
			
			FilterBaseEditor.OnInspectorGUI_Debug(this.target as FilterBase);

			serializedObject.ApplyModifiedProperties();
		}
	}
}