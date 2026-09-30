
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{

    [CustomEditor(typeof(ModulesVerify))]
    public class ModulesVerifyEditor : UnityEditor.Editor
    {
        private List<string> modulePaths = new();
        private Vector2 scroll;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            DrawModules();

            GUILayout.Space(10);
            if (GUILayout.Button("Verify Modules"))
            {
                Debug.Log("Verify button clicked in Inspector!");
                RunVerification();
            }
        }

        private void DrawModules()
        {
            bool everythingVerified = true;
            GUILayout.Label("Falcon Modules:", EditorStyles.boldLabel);
            foreach (var module in FModuleManager.Instance.modules.Values)
            {
                if (module.status)
                    continue;
                everythingVerified = false;
                EditorGUILayout.BeginVertical("box");
                GUIStyle boldStyle = new GUIStyle(EditorStyles.label);
                boldStyle.fontStyle = FontStyle.Bold;
                EditorGUILayout.LabelField("🧩" + module.name, (module.status ? "✅" : "❌"), GUILayout.ExpandWidth(true));

                foreach (var verifier in module.verified)
                {
                    
                    if (verifier.Value.Item1 && !verifier.Key.Contains("Author"))
                        continue;
                    string status = verifier.Value.Item1 ? "✅ " + verifier.Value.Item2 : "❌ " + verifier.Value.Item2;
                    EditorGUILayout.LabelField("     " + verifier.Key, status, GUILayout.ExpandWidth(true));

                } 
                EditorGUILayout.EndVertical();
            }

            if (everythingVerified)
            {
                EditorGUILayout.HelpBox("All modules are verified!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Some modules are not verified. Please check the details above.", MessageType.Warning);
            }
        }

        private void RunVerification()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            DrawModules();

            GUILayout.Space(10);
            if (GUILayout.Button("Verify Modules"))
            {
                Debug.Log("Verify button clicked in Inspector!");
                RunVerification();
            }
        }
    }
}

