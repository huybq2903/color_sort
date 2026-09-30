//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(ShineFilter), true)]
	[CanEditMultipleObjects]
	internal class ShineFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo = 
				new AboutInfo(s_aboutHelp, "UIFX - Shine Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/components/filters/shine-filter/"),
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

		private static readonly GUIContent Content_Animation = new GUIContent("Animation");
		private static readonly GUIContent Content_Falloff = new GUIContent("Falloff");
		private static readonly GUIContent Content_Trigger = new GUIContent("Trigger");
		private static readonly GUIContent Content_Shape = new GUIContent("Shape");
		private static readonly GUIContent Content_Mode = new GUIContent("Mode");
		private static readonly GUIContent Content_Position = new GUIContent("Position");

		private static readonly GUIContent Content_DeltaTime = new GUIContent("Delta Time");
		private static readonly GUIContent Content_Delay = new GUIContent("Delay");
		private static readonly GUIContent Content_Count = new GUIContent("Count");
		private static readonly GUIContent Content_Speed = new GUIContent("Speed");
		private static readonly GUIContent Content_Blend = new GUIContent("Blend");
		private static readonly GUIContent Content_GammaSpace = new GUIContent("Gamme Space");

		private SerializedProperty _propAngle;
		private SerializedProperty _propSize;
		private SerializedProperty _propSoftness;
		private SerializedProperty _propPower;

		private SerializedProperty _propMirror;
		private SerializedProperty _propFillMode;
		private SerializedProperty _propColor;
		private SerializedProperty _propGradient;
		private SerializedProperty _propTexture;
		private SerializedProperty _propReverseTexture;

		private SerializedProperty _propOffset;
		private SerializedProperty _propAnimationTrigger;
		private SerializedProperty _propScrollDeltaTime;
		private SerializedProperty _propScrollSpeed;
		private SerializedProperty _propScrollDelay;
		private SerializedProperty _propScrollCount;

		private SerializedProperty _propBlendMode;
		private SerializedProperty _propAdvancedBlendMode;
		private SerializedProperty _propAdvancedBlendModeGammaSpace;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;

		public override bool RequiresConstantRepaint()
		{
			var shineFilter = this.target as ShineFilter;
			if (!shineFilter.isActiveAndEnabled) return false;
			if (shineFilter.Strength <= 0f) return false;
			if (shineFilter.IsPreviewScroll && shineFilter.HasScrollSpeed())
			{
				EditorApplication.QueuePlayerLoopUpdate();
				return true;
			}
			return false;
		}

		void OnDisable()
		{
			foreach (var obj in this.targets)
			{
				var fill = obj as ShineFilter;
				// NOTE: fill can be null if it has been destroyed
				if (fill != null)
				{
					fill.IsPreviewScroll = false;
					fill.ResetScroll();
				}
			}
		}

		protected override void OnEnable()
		{
			_propAngle = VerifyFindProperty("_angle");
			_propSize = VerifyFindProperty("_size");
			_propSoftness = VerifyFindProperty("_softness");
			_propPower = VerifyFindProperty("_power");

			_propMirror = VerifyFindProperty("_mirror");
			_propFillMode = VerifyFindProperty("_fillMode");
			_propColor = VerifyFindProperty("_color");
			_propGradient = VerifyFindProperty("_gradient");
			_propTexture = VerifyFindProperty("_texture");
			_propReverseTexture = VerifyFindProperty("_reverseTexture");

			_propOffset = VerifyFindProperty("_offset");
			_propAnimationTrigger = VerifyFindProperty("_animationTrigger");
			_propScrollDeltaTime = VerifyFindProperty("_scrollDeltaTime");
			_propScrollSpeed = VerifyFindProperty("_scrollSpeed");
			_propScrollDelay = VerifyFindProperty("_scrollDelay");
			_propScrollCount = VerifyFindProperty("_scrollCount");

			_propBlendMode = VerifyFindProperty("_blendMode");
			_propAdvancedBlendMode = VerifyFindProperty("_advancedBlendMode");
			_propAdvancedBlendModeGammaSpace = VerifyFindProperty("_advancedBlendModeGammaSpace");
			_propStrength  = VerifyFindProperty("_strength");
			_propRenderSpace = VerifyFindProperty("_renderSpace");
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			var filter = this.target as FilterBase;
			var shineFilter = this.target as ShineFilter;

			if (OnInspectorGUI_Check(filter))
			{
				return;
			}

			{
				GUILayout.Label(Content_Shape, EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(_propAngle);
				EditorGUILayout.PropertyField(_propSize);
				EditorGUILayout.PropertyField(_propSoftness);
				EditorGUILayout.PropertyField(_propPower, Content_Falloff);
				EditorGUI.indentLevel++;
				GUILayout.Label(Content_Fill, EditorStyles.boldLabel);
				EditorGUI.indentLevel--;
			}
			{
				EditorGUILayout.PropertyField(_propMirror);
				EditorGUILayout.PropertyField(_propFillMode, Content_Mode);
				EditorGUI.indentLevel++;
				if (_propFillMode.intValue == (int)ShineFillMode.Color)
				{
					EditorGUILayout.PropertyField(_propColor);
				}
				else if (_propFillMode.intValue == (int)ShineFillMode.Gradient)
				{
					EditorGUILayout.PropertyField(_propGradient);
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel(Content_SpaceCharacter);
					if (GUILayout.Button(Content_Reverse))
					{
						var g = GetGradient(_propGradient);
						GradientUtils.Reverse(g);
						SetGradient(_propGradient, g);
					}
					EditorGUILayout.EndHorizontal();
				}
				else if (_propFillMode.intValue == (int)ShineFillMode.Texture)
				{
					EditorGUILayout.PropertyField(_propTexture);
					EditorGUILayout.PropertyField(_propReverseTexture, Content_Reverse);
				}
				EditorGUI.indentLevel--;
			}
			{
				GUILayout.Label(Content_Position, EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(_propOffset);
				EditorGUI.indentLevel--;
			}
			{
				GUILayout.Label(Content_Animation, EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				{
					EditorGUILayout.PropertyField(_propAnimationTrigger, Content_Trigger);
					EnumAsToolbarCompact(_propScrollDeltaTime, Content_DeltaTime);
					EditorGUILayout.PropertyField(_propScrollDelay, Content_Delay);
					EditorGUILayout.PropertyField(_propScrollCount, Content_Count);
					GUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(_propScrollSpeed, Content_Speed);
					if (!EditorApplication.isPlaying)
					{
						if (ToggleButton(shineFilter.IsPreviewScroll, Content_Stop, Content_Preview))
						{
							if (shineFilter.IsPreviewScroll)
							{
								foreach (var obj in this.targets)
								{
									var fillInstance = obj as ShineFilter;
									fillInstance.IsPreviewScroll = false;
									fillInstance.ResetScroll();
								}
							}
							else
							{
								foreach (var obj in this.targets)
								{
									var fillInstance = obj as ShineFilter;
									fillInstance.IsPreviewScroll = true;
								}
							}
							EditorApplication.QueuePlayerLoopUpdate();
						}
					}
					GUILayout.EndHorizontal();
				}
				EditorGUI.indentLevel--;
			}
			{
				GUILayout.Label(Content_Blend, EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				EnumAsToolbar(_propBlendMode);
				if (_propBlendMode.intValue == (int)ShineBlendMode.Advanced)
				{
					EnumWithButtons(_propAdvancedBlendMode);
					EditorGUILayout.PropertyField(_propAdvancedBlendModeGammaSpace, Content_GammaSpace);
				}
				EditorGUI.indentLevel--;
			}
			{
				GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
				EditorGUI.indentLevel++;
				DrawFilterRenderSpaceControl(_propRenderSpace);
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