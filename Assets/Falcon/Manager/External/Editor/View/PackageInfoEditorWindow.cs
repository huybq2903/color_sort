
/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-23
     */


using UnityEditor;
using UnityEngine;

namespace Falcon.Manager.External
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;
    using Falcon.Manager.Shared;
    using Newtonsoft.Json;

    public class PackageInfoEditorWindow : EditorWindow
    {
        private string _newVersion;
        private string _newLog = "New changes";
        private Vector2 _scrollPosition;
        
        private static  AuthRegistryEntry _package;

        internal static Action OnUpdatePackage;

        public static void OpenWindow(AuthRegistryEntry package)
        {
            _package = package;
            var window = GetWindow<PackageInfoEditorWindow>("Package Info Editor");
            window.minSize = new Vector2(300, 300);
        }

        private void OnEnable()
        {
            _newVersion = _package.version;
        }

        private void OnGUI()
        {
            if (_package == null)
            {
                EditorGUILayout.LabelField($"Package is Null. Please re-open this window.");
                return;
            }
            
            EditorGUILayout.LabelField($"Module: {_package.displayName}", WindowStyles.TextBoldColor(Color.white));
            
            GUILayout.Space(5);
            _newVersion = EditorGUILayout.TextField($"Version: ", _newVersion);
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Changelog");
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _newLog         = EditorGUILayout.TextArea(_newLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            GUI.enabled = !string.IsNullOrEmpty(_newVersion) && _newVersion != _package.version;
            if (GUILayout.Button("Update New Version Info", GUILayout.Height(30)))
            {
                _newVersion = _newVersion.Trim();
                
                if (!Regex.IsMatch(_newVersion, @"^\d+\.\d+\.\d+$"))
                {
                    _newVersion = _package.version;
                    EditorUtility.DisplayDialog("Error", "Invalid version format. Please use format like 1.0.0\"", "Ok");
                    return;
                }

                var newVersionParsed = new Version(_newVersion);
                var currentVersionParsed = new Version(_package.version);

                if (newVersionParsed <= currentVersionParsed)
                {
                    EditorUtility.DisplayDialog("Error", $"New version ({_newVersion}) must be greater than current version ({_package.version})", "Ok");
                    return;
                }
                
                var    packageJsonPath = Path.Combine(_package.packagePath, "package.json");
                string jsonContent     = File.ReadAllText(packageJsonPath);
                var    packageData     = JsonConvert.DeserializeObject<LocalFalconPackage>(jsonContent);

                packageData.version = _newVersion;
                
                var json = JsonConvert.SerializeObject(packageData, Formatting.Indented);
                File.WriteAllText(packageJsonPath, json);
                AssetDatabase.Refresh();
                
                if (string.IsNullOrEmpty(_newLog))
                {
                    _newLog = "New changes";
                }

                _newLog  = _newLog.Trim();
                
                var changelogPath = Path.Combine(_package.packagePath, "CHANGELOG.md");
                ChangelogManager.AddNewVersion(_newVersion, _newLog, changelogPath);
                Close();
                
                OnUpdatePackage?.Invoke();
            }

            GUI.enabled = true;
        }
    }
} 