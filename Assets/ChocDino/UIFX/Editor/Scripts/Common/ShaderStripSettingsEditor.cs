//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(ShaderStripSettings))]
	public class ShaderStripSettingsEditor : BaseEditor
	{
		private SerializedProperty _propEnabled;
		private SerializedProperty _propShaderStripNames;
		private List<string> _effectNames = new List<string>(32);
		private List<string> _strippedEffectNames = new List<string>(32);

		private static readonly GUIContent Content_All = new GUIContent("All");
		private static readonly GUIContent Content_Space = new GUIContent(" ");
		private static readonly GUIContent Content_ShadersUsed = new GUIContent("Shaders used");
		private static readonly GUIContent Content_EnableStripping = new GUIContent("UIFX Shader Stripping");
		private static readonly GUIContent Content_SelectStripComponents = new GUIContent("Select UIFX components to strip:");

		void OnEnable()
		{
			_propEnabled = VerifyFindProperty("_enabled");
			_propShaderStripNames = VerifyFindProperty("_shaderStripEffectNames");
			CollectEffectNames();
		}

		void CollectEffectNames()
		{
			_effectNames.Clear();
			_strippedEffectNames.Clear();

			foreach (var shaderUsage in ShaderUsageRegistry.All)
			{
				_effectNames.Add(shaderUsage.Id);
			}

			var enumerator = _propShaderStripNames.GetEnumerator();
			while (enumerator.MoveNext())
			{
				var propShaderStripName = enumerator.Current as SerializedProperty;
				string effectName = propShaderStripName.stringValue;
				if (!string.IsNullOrEmpty(effectName))
				{
					if (!_effectNames.Contains(effectName))
					{
						_effectNames.Add(effectName);
					}
					_strippedEffectNames.Add(effectName);
				}
			}
			_effectNames.Sort();
		}

		bool IsEffectStripped(ShaderUsage shaderUsage)
		{
			return IsEffectStripped(shaderUsage.Id);
		}

		void AddStripped(ShaderUsage shaderUsage)
		{
			AddStripped(shaderUsage.Id);
		}

		void RemoveStripped(ShaderUsage shaderUsage)
		{
			RemoveStripped(shaderUsage.Id);
		}

		bool IsEffectStripped(string effectName)
		{
			return _strippedEffectNames.Contains(effectName);
		}

		void AddStripped(string effectName)
		{
			Debug.Assert(!_strippedEffectNames.Contains(effectName));
			_strippedEffectNames.Add(effectName);
			UpdateStrippedProperty();
		}

		void RemoveStripped(string effectName)
		{
			Debug.Assert(_strippedEffectNames.Contains(effectName));
			_strippedEffectNames.Remove(effectName);
			UpdateStrippedProperty();
		}

		void SetAllStripped()
		{
			_strippedEffectNames = new List<string>(_effectNames);
			UpdateStrippedProperty();
		}

		void SetNoneStripped()
		{
			_strippedEffectNames.Clear();
			UpdateStrippedProperty();
		}

		void UpdateStrippedProperty()
		{
			_propShaderStripNames.ClearArray();
			_propShaderStripNames.arraySize = _strippedEffectNames.Count;
			for (int i = 0; i < _propShaderStripNames.arraySize; i++)
			{
				_propShaderStripNames.GetArrayElementAtIndex(i).stringValue = _strippedEffectNames[i];
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var settings = this.target as ShaderStripSettings;
			int shaderTotalCount = 0;
			if (settings != null)
			{
				shaderTotalCount = ShaderUsageRegistry.UniqueShaders.Count;
			}

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(_propEnabled, Content_EnableStripping);
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(!_propEnabled.boolValue);
			{
				GUILayout.Label(Content_SelectStripComponents, EditorStyles.textField);
				EditorGUILayout.Space();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Toggle(Content_Space, false, GUI.skin.label, GUILayout.ExpandWidth(false));
				GUILayout.Space(32f);
				GUILayout.Label(Content_ShadersUsed);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();

				bool isAll = _effectNames.Count == _strippedEffectNames.Count;
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginChangeCheck();
				bool isNowAll = EditorGUILayout.Toggle(Content_All, isAll, GUILayout.ExpandWidth(false));
				if (EditorGUI.EndChangeCheck())
				{
					if (isNowAll)
					{
						SetAllStripped();
					}
					else
					{
						SetNoneStripped();
					}
				}

				GUILayout.Space(32f);
				if (settings != null)
				{
					int shaderKeepCount = settings.GetShadersToKeep().Count;
					GUILayout.Label(shaderKeepCount + "/" + shaderTotalCount, EditorStyles.boldLabel);
				}
				else
				{
					GUILayout.Label(shaderTotalCount.ToString(), EditorStyles.boldLabel);
				}

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();

				ShowCategory(ShaderUsageCategory.Filters);
				ShowCategory(ShaderUsageCategory.Effects);
				ShowCategory(ShaderUsageCategory.Sources);
				ShowCategory(ShaderUsageCategory.Utilities);
				ShowCategory(ShaderUsageCategory.UIToolkitFilters);
				//ShowCategory(ShaderUsageCategory.Others);
			}
			EditorGUI.EndDisabledGroup();

			// Display property for debugging.
			//EditorGUILayout.Space();
			//EditorGUILayout.PropertyField(_propShaderStripNames);

			serializedObject.ApplyModifiedProperties();
		}

		void ShowCategory(ShaderUsageCategory category)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(category.ToString(), EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel++;
			foreach (var shaderUsage in ShaderUsageRegistry.All)
			{
				if (category == shaderUsage.Category)
				{
					bool isStripped = IsEffectStripped(shaderUsage);

					EditorGUILayout.BeginHorizontal();

					EditorGUI.BeginChangeCheck();
					bool isStrippedNow = EditorGUILayout.Toggle(shaderUsage.Name, isStripped, GUILayout.ExpandWidth(false));
					if (EditorGUI.EndChangeCheck())
					{
						if (isStrippedNow)
						{
							AddStripped(shaderUsage);
						}
						else
						{
							RemoveStripped(shaderUsage);
						}
					}

					GUILayout.Space(32f);
					GUILayout.Label(shaderUsage.ShaderCount.ToString());
					
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUI.indentLevel--;
			EditorGUILayout.Space();
		}
	}
}