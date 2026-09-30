using UnityEditor;
using UnityEngine;

namespace I2.Loc
{
#if UNITY_EDITOR
    [CustomEditor(typeof(LocalizeDataSourceModules))]
    public class LocalizeDataSourceModulesEditor : Editor
    {
        [MenuItem("Falcon/Tools/Build I2 Localize Terms")]
        public static void OpenBuildI2LocalizeTerms()
        {
            string filePath = "Assets/Falcon/Tools/I2/Resources/I2DataNameSourceModules.asset";
            var target = AssetDatabase.LoadAssetAtPath<LocalizeDataSourceModules>(filePath);
            if (target != null)
            {
                Selection.activeObject = target;
                EditorGUIUtility.PingObject(target);
                GetSources(target);
            }
            else
            {
                Debug.LogError("Could not find file: " + filePath);
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Fetch All Terms", GUILayout.Height(30)))
            {
                GetSources((LocalizeDataSourceModules)target);
            }
        }

        public static void GetSources(LocalizeDataSourceModules data)
        {
            data.modules.Clear();

            string[] guids = AssetDatabase.FindAssets("t:LanguageSourceAsset", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var source = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(path);
                if (source != null && !data.modules.Contains(source))
                {
                    source.SourceData.GoogleUpdateFrequency = LanguageSourceData.eGoogleUpdateFrequency.Never;
                    source.SourceData.GoogleInEditorCheckFrequency = LanguageSourceData.eGoogleUpdateFrequency.Never;
                    source.SourceData.GoogleUpdateSynchronization = LanguageSourceData.eGoogleUpdateSynchronization.Manual;

                    // Mark the source as dirty and save changes
                    data.modules.Add(source);
                }
            }

            // Mark the source as dirty and save changes
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }
#endif
}
