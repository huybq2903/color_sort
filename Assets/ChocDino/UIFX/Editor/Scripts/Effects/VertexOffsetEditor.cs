//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(VertexOffset))]
	[CanEditMultipleObjects]
	internal class VertexOffsetEditor : BaseEditor
	{
		private static readonly GUIContent Content_Apply = new GUIContent("Apply");
		private static readonly GUIContent Content_Trigger = new GUIContent("Trigger");
		private static readonly GUIContent Content_Animation = new GUIContent("Animation");
		private static readonly GUIContent Content_Speed = new GUIContent("Speed");
		private static readonly GUIContent Content_DeltaTime = new GUIContent("Delta Time");
		protected static readonly GUIContent Content_Preview = new GUIContent("Preview");
		protected static readonly GUIContent Content_Stop = new GUIContent("Stop");

		private SerializedProperty _propOffset;
		private SerializedProperty _propEasing;
//		private SerializedProperty _propAnimTrigger;
//		private SerializedProperty _propAnimDeltaTime;
//		private SerializedProperty _propAnimSpeed;
		private SerializedProperty _propStrength;

		void OnEnable()
		{
			_propOffset = VerifyFindProperty("_offset");
			_propEasing = VerifyFindProperty("_easing");
			//_propAnimTrigger = VerifyFindProperty("_animTrigger");
			//_propAnimDeltaTime = VerifyFindProperty("_animDeltaTime");
			//_propAnimSpeed = VerifyFindProperty("_animSpeed");
			_propStrength = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var effect = this.target as VertexOffset;

			EditorGUILayout.PropertyField(_propOffset);

			/*GUILayout.Label(Content_Animation, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			GUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(_propAnimSpeed, Content_Speed);
			if (!EditorApplication.isPlaying)
			{
				if (ToggleButton(effect.IsPreviewAnim, Content_Stop, Content_Preview))
				{
					if (effect.IsPreviewAnim)
					{
						foreach (var obj in this.targets)
						{
							var effectInstance = obj as VertexOffset;
							effectInstance.IsPreviewAnim = false;
							effectInstance.ResetAnim();
						}
					}
					else
					{
						foreach (var obj in this.targets)
						{
							var effectInstance = obj as VertexOffset;
							effectInstance.IsPreviewAnim = true;
						}
					}
					EditorApplication.QueuePlayerLoopUpdate();
				}
			}
			GUILayout.EndHorizontal();
			EditorGUI.BeginDisabledGroup(_propAnimSpeed.floatValue <= 0f);
				
			EditorGUILayout.PropertyField(_propAnimTrigger, Content_Trigger);
			EnumAsToolbarCompact(_propAnimDeltaTime, Content_DeltaTime);

			EditorGUI.EndDisabledGroup();
			EditorGUI.indentLevel--;*/

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propEasing);
			EditorGUILayout.PropertyField(_propStrength);
			EditorGUI.indentLevel--;

			serializedObject.ApplyModifiedProperties();
		}

		public override bool RequiresConstantRepaint()
		{
			var effect = this.target as VertexOffset;
			if (!effect.isActiveAndEnabled) return false;
			if (effect.Strength <= 0f) return false;
			/*if (effect.IsPreviewAnimation && effect.HasScrollSpeed())
			{
				EditorApplication.QueuePlayerLoopUpdate();
				return true;
			}*/
			return false;
		}

		/*void OnDisable()
		{
			foreach (var obj in this.targets)
			{
				var effect = obj as VertexOffset;
				// NOTE: effect can be null if it has been destroyed
				if (effect != null)
				{
					effect.IsPreviewAnimation = false;
					effect.ResetAnimation();
				}
			}
		}*/
	}
}