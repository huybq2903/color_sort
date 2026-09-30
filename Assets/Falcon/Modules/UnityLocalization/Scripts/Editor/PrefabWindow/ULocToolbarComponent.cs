/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-22
 */

using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.UnityLocalization.Editor
{
    public class ULocToolbarComponent : IEditorComponent
    {
        public string Search { get; private set; } = "";

        public void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Search:", GUILayout.Width(50));
                Search = GUILayout.TextField(Search, EditorStyles.toolbarTextField, GUILayout.MinWidth(120));
                GUILayout.Space(10);
                GUILayout.FlexibleSpace();
            }
        }
    }

}