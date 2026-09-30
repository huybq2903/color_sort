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
    public class CreateLocalModuleWindow : EditorWindow
    {
        private string _moduleId = "falcon.modules.";
        private string _moduleName = "New Module";

        public static void OpenWindow()
        {
            var window = GetWindow<CreateLocalModuleWindow>("Create Local Module");
            window.minSize = new Vector2(400, 150);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Enter module details:", EditorStyles.boldLabel);
            GUILayout.Space(10);

            _moduleId = EditorGUILayout.TextField("Module ID", _moduleId);
            _moduleName = EditorGUILayout.TextField("Display Name", _moduleName);

            GUILayout.Space(20);

            if (GUILayout.Button("Create", GUILayout.Height(30)))
            {
                CreateModule();
            }
        }

        private void CreateModule()
        {
            const string prefix = "falcon.modules.";
            if (string.IsNullOrWhiteSpace(_moduleId) || !_moduleId.StartsWith(prefix) || _moduleId.Length <= prefix.Length || _moduleId.Contains(" "))
            {
                EditorUtility.DisplayDialog("Invalid ID", $"Module ID must start with '{prefix}', contain no spaces, and have a name.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_moduleName))
            {
                EditorUtility.DisplayDialog("Invalid Name", "Module Name cannot be empty.", "OK");
                return;
            }
            
            LocalPackageRepository.Refresh();
            if (LocalPackageRepository.Packages.ContainsKey(_moduleId))
            {
                EditorUtility.DisplayDialog("Error", $"A module with ID '{_moduleId}' already exists.", "OK");
                return;
            }

            var creationService = new LocalPackageCreationService();
            var success = creationService.CreateModule(_moduleId, _moduleName, out var path);

            if (success)
            {
                EditorUtility.DisplayDialog("Success", $"Module '{_moduleName}' created successfully.", "OK");
                LocalPackageRepository.Refresh();
                PackageInfoEditorWindow.OnUpdatePackage?.Invoke();
                
                Object folderObject = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (folderObject != null) EditorGUIUtility.PingObject(folderObject);
                
                Close();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to create module. Check console for details.", "OK");
            }
        }
    }
}
