/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-22
 */

using System;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.UnityLocalization.Editor
{
    public class ULocHeaderComponent : IEditorComponent
    {
        private GameObject _currentRoot;
        private Action _onRefresh;

        public ULocHeaderComponent(GameObject root, Action onRefresh)
        {
            _currentRoot = root;
            _onRefresh = onRefresh;
        }

        public void SetRoot(GameObject root) => _currentRoot = root;

        public void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Prefab Localization", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Current Root:", GUILayout.Width(110));
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField(_currentRoot, typeof(GameObject), true);
                    GUI.enabled = true;

                    if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                        _onRefresh?.Invoke();
                }
            }
        }
    }

}