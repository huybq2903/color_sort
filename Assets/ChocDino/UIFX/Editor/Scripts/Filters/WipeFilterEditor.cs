//--------------------------------------------------------------------------//
// Copyright 2023-2025 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace ChocDino.UIFX.Editor
{
	[CustomEditor(typeof(WipeFilter), true)]
	[CanEditMultipleObjects]
	internal class WipeFilterEditor : FilterBaseEditor
	{
		private static readonly AboutInfo s_aboutInfo =
				new AboutInfo(s_aboutHelp, "UIFX - Wipe Filter\n© Chocolate Dinosaur Ltd", "uifx-icon")
				{
					sections = new AboutSection[]
					{
						new AboutSection("Asset Guides")
						{
							buttons = new AboutButton[]
							{
								new AboutButton("Components Reference", "https://www.chocdino.com/products/uifx/components/filters/wipe-filter/"),
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

		private static readonly AboutToolbar s_aboutToolbar = new AboutToolbar(new AboutInfo[] { s_upgradeFromStarterBundle, s_upgradeFromIndieBundle, s_aboutInfo });

		private static readonly GUIContent Content_Wipe = new GUIContent("Wipe");
		private static readonly GUIContent Content_Invert = new GUIContent("Invert Direction");

		private SerializedProperty _propWipeInstance;
		private SerializedProperty _propInvertDirection;
		private SerializedProperty _propEasing;
		private SerializedProperty _propStrength;
		private SerializedProperty _propRenderSpace;

		private List<System.Type> _wipeTypes;
		private GUIContent[] _wipeTypeNames;
		private static GUIStyle _wipeStyleLabelStyle;

		protected override void OnEnable()
		{
			var extractedTypes = TypeCache.GetTypesDerivedFrom<WipeBase>();
			if (extractedTypes.Count > 0)
			{
				_wipeTypes = extractedTypes.OrderBy(type => type.Name).ToList();
				_wipeTypeNames = new GUIContent[_wipeTypes.Count];
				for (int i = 0; i < _wipeTypes.Count; i++)
				{
					_wipeTypeNames[i] = new GUIContent(_wipeTypes[i].Name.Replace("Wipe", string.Empty));
				}
			}
			else
			{
				_wipeTypeNames = new GUIContent[0];
			}

			_propWipeInstance = VerifyFindProperty("_wipeInstance");
			_propInvertDirection = VerifyFindProperty("_invertDirection");
			_propEasing = VerifyFindProperty("_easing");
			_propStrength = VerifyFindProperty("_strength");
			_propRenderSpace = VerifyFindProperty("_renderSpace");
			base.OnEnable();
		}

		private void OnDisable()
		{
			_wipeTypes = null;
			_wipeTypeNames = null;
		}

		private void OnMenuOptionSelected(object userData)
		{
			var wipeType = _wipeTypes[(int)userData];

			Undo.RecordObjects(this.targets, "Create Wipe Object Instance");

			foreach (var obj in this.targets)
			{
				var wipeFilter = obj as WipeFilter;
				wipeFilter.SetWipe((WipeBase)System.Activator.CreateInstance(wipeType));

				if (PrefabUtility.IsPartOfPrefabInstance(wipeFilter.gameObject))
				{
					// Register the property change specifically for the Prefab overrides system
					PrefabUtility.RecordPrefabInstancePropertyModifications(wipeFilter);
				}
				else
				{
					// Fallback for standard scene objects to ensure changes persist on save
					EditorUtility.SetDirty(wipeFilter.gameObject);
				}
			}
		}

		public override void OnInspectorGUI()
		{
			s_aboutToolbar.OnGUI();

			serializedObject.Update();

			var filter = this.target as FilterBase;
			var wipeFilter = this.target as WipeFilter;

			if (OnInspectorGUI_Check(filter))
			{
				return;
			}

			GUILayout.Label(Content_Wipe, EditorStyles.boldLabel);
			if (_wipeStyleLabelStyle == null)
			{
				_wipeStyleLabelStyle = new GUIStyle(EditorStyles.label);
				_wipeStyleLabelStyle.fontSize = 14;
				_wipeStyleLabelStyle.alignment = TextAnchor.MiddleCenter;
			}

			EditorGUI.indentLevel++;
			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Style");
			if (_wipeTypeNames != null && _wipeTypeNames.Length > 0)
			{
				if (GUILayout.Button(new GUIContent(GetWipeName(_propWipeInstance)), EditorStyles.popup))
				{
					GenericMenu menu = new GenericMenu();
					for (int i = 0; i < _wipeTypes.Count; i++)
					{
						menu.AddItem(_wipeTypeNames[i], false, OnMenuOptionSelected, i);
					}
					menu.ShowAsContext();
				}
			}
			GUILayout.EndHorizontal();

			if (!string.IsNullOrEmpty(_propWipeInstance.managedReferenceFullTypename))
			{
				EditorGUILayout.PropertyField(_propWipeInstance, new GUIContent(GetWipeName(_propWipeInstance)), true);
			}

			EditorGUILayout.PropertyField(_propInvertDirection, Content_Invert);
			EditorGUI.indentLevel--;

			GUILayout.Label(Content_Apply, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EnumAsToolbarCompact(_propRenderSpace);
			EditorGUILayout.PropertyField(_propEasing);
			DrawStrengthProperty(_propStrength);
			EditorGUI.indentLevel--;

			// TODO: Add these buttons
			/*GUILayout.BeginHorizontal();
			GUILayout.Button("Wipe In");
			GUILayout.Button("Wipe Out");
			GUILayout.EndHorizontal();*/

			if (OnInspectorGUI_Baking(filter))
			{
				return;
			}

			FilterBaseEditor.OnInspectorGUI_Debug(filter);

			serializedObject.ApplyModifiedProperties();
		}

		public static string GetWipeName(SerializedProperty property)
		{
			string fullType = property.managedReferenceFullTypename;

			if (string.IsNullOrEmpty(fullType))
				return "Null";

			// Split by spaces to separate the Assembly Name from the Type Path
			// "Assembly-CSharp ChocDino.UIFX.WipeBlinds" -> "ChocDino.UIFX.WipeBlinds"
			string typePath = fullType.Split(' ').Last();

			// Split by dots to get just the final class name token
			return typePath.Split('.').Last().Replace("Wipe", string.Empty);
		}
	}
}