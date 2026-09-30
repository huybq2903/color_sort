/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-22
 */

// ULocEntryComponent.cs (Editor)

using Falcon.Modules.UnityLocalization.Editor;
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization;
using UnityEditor.Localization.UI;

public class ULocEntryComponent : IEditorComponent
{
    private StringTableCollection _tableCollection;
    private string _newEntryName = "";
    private Editor _tableCollectionEditor;
    private bool _showTableInspector; // trạng thái mở/thu

    private const string PREF_KEY = "ULocEntryComponent_TableCollection";

    public ULocEntryComponent()
    {
        LoadTableReference();
    }

    public void OnGUI()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.BeginHorizontal();

            var newTable = (StringTableCollection)EditorGUILayout.ObjectField(
                "String Table Collection",
                _tableCollection,
                typeof(StringTableCollection),
                false
            );

            if (newTable != _tableCollection)
            {
                _tableCollection = newTable;
                SaveTableReference();
                // Dispose old editor when table changes
                if (_tableCollectionEditor != null)
                {
                    Object.DestroyImmediate(_tableCollectionEditor);
                    _tableCollectionEditor = null;
                }
            }

            using (new EditorGUI.DisabledScope(_tableCollection == null))
            {
                if (GUILayout.Button("Open", GUILayout.Width(60)))
                    LocalizationTablesWindow.ShowWindow(_tableCollection);
            }

            EditorGUILayout.EndHorizontal();

            _newEntryName = EditorGUILayout.TextField("Entry Name", _newEntryName);

            if (_tableCollection)
            {
                if (!string.IsNullOrEmpty(_newEntryName) &&
                    GUILayout.Button("Add Table Entry", GUILayout.Height(24)))
                {
                    Undo.RecordObject(_tableCollection, "Add Table Entry");
                    _tableCollection.SharedData.AddKey(_newEntryName);
                    EditorUtility.SetDirty(_tableCollection);
                    AssetDatabase.SaveAssets();

                    Debug.Log($"✅ Added entry '{_newEntryName}' to table '{_tableCollection.name}'");
                    _newEntryName = "";
                }

                if (GUILayout.Button("Generate Script", GUILayout.Height(24)))
                {
                    ULocGeneratorScript.BuildCodeForCollection(_tableCollection);
                }
            }


            DrawTableCollectionInspector();
        }
    }

    private Vector2 _inspectorScrollPos;

    private void DrawTableCollectionInspector()
    {
        if (!_tableCollection)
        {
            if (_tableCollectionEditor)
            {
                Object.DestroyImmediate(_tableCollectionEditor);
                _tableCollectionEditor = null;
            }

            return;
        }

        _showTableInspector = EditorGUILayout.BeginFoldoutHeaderGroup(
            _showTableInspector,
            "String Table Collection Inspector"
        );

        if (_showTableInspector)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.85f, 0.95f, 1f);

            EditorGUILayout.BeginVertical("box");

            _inspectorScrollPos = EditorGUILayout.BeginScrollView(_inspectorScrollPos);

            // Cache editor instance with validation
            if (!_tableCollectionEditor || _tableCollectionEditor.target != _tableCollection)
            {
                if (_tableCollectionEditor)
                    Object.DestroyImmediate(_tableCollectionEditor);
                _tableCollectionEditor = Editor.CreateEditor(_tableCollection);
            }

            if (_tableCollectionEditor)
            {
                EditorGUI.indentLevel++;
                _tableCollectionEditor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = oldColor;
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
    }


    private void SaveTableReference()
    {
        if (_tableCollection)
        {
            var path = AssetDatabase.GetAssetPath(_tableCollection);
            var guid = AssetDatabase.AssetPathToGUID(path);
            EditorPrefs.SetString(PREF_KEY, guid);
        }
        else
        {
            EditorPrefs.DeleteKey(PREF_KEY);
        }
    }

    private void LoadTableReference()
    {
        if (EditorPrefs.HasKey(PREF_KEY))
        {
            var guid = EditorPrefs.GetString(PREF_KEY);
            var path = AssetDatabase.GUIDToAssetPath(guid);
            _tableCollection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);
        }
    }
}
