//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(VertexSkew))]
	[CanEditMultipleObjects]
	internal class VertexSkewEditor : BaseEditor
	{
		private static readonly GUIContent Content_Skew = new GUIContent("Skew");
		private static readonly GUIContent Content_Pivot = new GUIContent("Pivot");
		private static readonly GUIContent Content_Bounds = new GUIContent("Bounds");
		private static readonly GUIContent Content_Apply = new GUIContent("Apply");

		private SerializedProperty _propPivotBounds;
		private SerializedProperty _propPivot;
		private SerializedProperty _propDirection;
		private SerializedProperty _propAngle;
		private SerializedProperty _propOffset;
		private SerializedProperty _propStrength;

		void OnEnable()
		{
			_propPivotBounds = VerifyFindProperty("_pivotBounds");
			_propPivot = VerifyFindProperty("_pivot");
			_propDirection = VerifyFindProperty("_direction");
			_propAngle = VerifyFindProperty("_angle");
			_propOffset = VerifyFindProperty("_offset");
			_propStrength = VerifyFindProperty("_strength");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUILayout.Label(Content_Pivot, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EnumAsToolbar(_propPivotBounds, Content_Bounds);
			EditorGUILayout.PropertyField(_propPivot);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Skew, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EnumAsToolbar(_propDirection);
			EditorGUILayout.PropertyField(_propAngle);
			EditorGUILayout.PropertyField(_propOffset);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(_propStrength);
			EditorGUI.indentLevel--;

			serializedObject.ApplyModifiedProperties();
		}

		public void OnSceneGUI()
		{
			const float size = 200f;
			if (Event.current.type == EventType.Repaint)
			{
				Transform transform = ((VertexSkew)target).transform;

				var skew = target as VertexSkew;

				if (!skew.isActiveAndEnabled) return;
				if (skew.Strength <= 0f) return;

				// Draw line for the pivot
				Handles.matrix = transform.localToWorldMatrix;
				Handles.color = Handles.yAxisColor;
				if (skew.Direction == SkewDirection.Horizontal)
				{
					Handles.DrawLine(new Vector3(skew.BoundsMin.x, skew.PivotPoint.y, 0f), new Vector3(skew.BoundsMax.x, skew.PivotPoint.y, 0f));
				}
				else
				{
					Handles.DrawLine(new Vector3(skew.PivotPoint.x, skew.BoundsMin.y, 0f), new Vector3(skew.PivotPoint.x, skew.BoundsMax.y, 0f));
				}

				float angle = skew.Angle;
				if (skew.Direction == SkewDirection.Horizontal)
				{
					angle = -angle;
				}

				// Draw arrow showing offset direction from pivot
				Matrix4x4 m = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f,  angle));
				Handles.matrix = transform.localToWorldMatrix *  Matrix4x4.Translate(skew.PivotPoint) * m;
				Handles.color = Handles.yAxisColor;
				if (skew.Direction == SkewDirection.Horizontal)
				{
					Handles.ArrowHandleCap(
						0,
						Vector3.zero,
						Quaternion.LookRotation(Vector3.up),
						size,
						EventType.Repaint
					);
				}
				else
				{			
					Handles.ArrowHandleCap(
						0,
						Vector3.zero,
						Quaternion.LookRotation(Vector3.right),
						size,
						EventType.Repaint
					);
				}
			}
		}
	}
}