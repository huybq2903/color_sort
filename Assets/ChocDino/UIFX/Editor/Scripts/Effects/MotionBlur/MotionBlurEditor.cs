//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(MotionBlurSimple), true)]
	[CanEditMultipleObjects]
	internal class MotionBlurSimpleEditor : MotionBlurEditor
	{
		private SerializedProperty _propMode;
		private SerializedProperty _propSampleCount;
		private SerializedProperty _propBlendStrength;
		private SerializedProperty _propLerpUV;
		private SerializedProperty _propFrameRateIndependent;
		private SerializedProperty _propStrength;

		void OnEnable()
		{
			_propMode = VerifyFindProperty("_mode");
			_propSampleCount = VerifyFindProperty("_sampleCount");
			_propBlendStrength = VerifyFindProperty("_blendStrength");
			_propLerpUV = VerifyFindProperty("_lerpUV");
			_propFrameRateIndependent = VerifyFindProperty("_frameRateIndependent");
			_propStrength = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			EditorGUILayout.PropertyField(_propMode);
			EditorGUILayout.PropertyField(_propSampleCount);
			EditorGUILayout.PropertyField(_propBlendStrength);
			EditorGUILayout.PropertyField(_propLerpUV);
			EditorGUILayout.PropertyField(_propFrameRateIndependent);
			EditorGUILayout.PropertyField(_propStrength);
			
			serializedObject.ApplyModifiedProperties();
		}
	}

	[CustomEditor(typeof(MotionBlurReal), true)]
	[CanEditMultipleObjects]
	internal class MotionBlurRealEditor : MotionBlurEditor
	{
		private SerializedProperty _propMode;
		private SerializedProperty _propSampleCount;
		private SerializedProperty _propLerpUV;
		private SerializedProperty _propFrameRateIndependent;
		private SerializedProperty _propStrength;
		private SerializedProperty _propShaderAdd;
		private SerializedProperty _propShaderResolve;

		void OnEnable()
		{
			_propMode = VerifyFindProperty("_mode");
			_propSampleCount = VerifyFindProperty("_sampleCount");
			_propLerpUV = VerifyFindProperty("_lerpUV");
			_propFrameRateIndependent = VerifyFindProperty("_frameRateIndependent");
			_propStrength = VerifyFindProperty("_strength");
			_propShaderAdd = VerifyFindProperty("_shaderAdd");
			_propShaderResolve = VerifyFindProperty("_shaderResolve");
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			EditorGUILayout.PropertyField(_propMode);
			EditorGUILayout.PropertyField(_propSampleCount);
			EditorGUILayout.PropertyField(_propLerpUV);
			EditorGUILayout.PropertyField(_propFrameRateIndependent);
			EditorGUILayout.PropertyField(_propStrength);
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(_propShaderAdd);
			EditorGUILayout.PropertyField(_propShaderResolve);

			serializedObject.ApplyModifiedProperties();
		}
	}

	internal abstract class MotionBlurEditor : BaseEditor
	{
		protected static readonly AboutInfo s_aboutInfo
				= new AboutInfo(s_aboutHelp, "UIFX - Motion Blur\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Documentation")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/components/effects/motion-blur-real/"),
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
								new AboutButton("Post to Unity Forum Thread", "https://discussions.unity.com/t/released-uifx-motion-blur/930437"),
								new AboutButton("Post Issues to GitHub", GithubUrl),
								new AboutButton("Email Us", SupportEmailUrl),
							}
						}
					}
				};
		
		internal static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeFromStarterBundle, s_upgradeFromIndieBundle, s_aboutInfo } );

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			DrawDefaultInspector();

			serializedObject.ApplyModifiedProperties();
		}
	}
}