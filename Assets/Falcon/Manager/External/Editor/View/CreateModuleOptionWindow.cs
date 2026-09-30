/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-10-16
     */


using Falcon.Manager.Shared;
using UnityEditor;
using UnityEngine;

namespace Falcon.Manager.External
{
    public class CreateModuleOptionWindow : EditorWindow
    {
        public static void OpenWindow()
        {
            var window = GetWindow<CreateModuleOptionWindow>("Create Module");
            window.minSize = new Vector2(300, 120);
            window.maxSize = new Vector2(300, 120);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Steps to create a module", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("1. Create on CMS", GUILayout.Height(30)))
            {
                var webUrl = $"{Configuration.CMS_WEB_URL}/unity-module/unity-modules";
                Application.OpenURL(webUrl);
            }

            if (GUILayout.Button("2. Create Locally", GUILayout.Height(30)))
            {
                CreateLocalModuleWindow.OpenWindow();
                Close();
            }
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField("*packageName (id) must be the same", EditorStyles.whiteMiniLabel);
            GUILayout.Space(5);
        }
    }
}
