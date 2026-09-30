using Falcon.Helpers.ConfigImporter.Editor;
using Falcon.Modules.UI.Level.Runtime;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.UI.Level.Editor
{
    public static class UILevelEditor
    {
        [MenuItem("Falcon/Modules/UI/Level/Assets")]
        private static void ImportMyModuleConfig()
        {
            var modulePath = "Falcon/Modules/UI/UILevel";
            ConfigImporter.ImportConfig(modulePath);
        }

        [MenuItem("Falcon/Modules/UI/Level/Config")]
        private static void OpenConfig()
        {
            var configs = Resources.Load<UILevelConfig>("UILevelConfig");
            if (configs == null)
            {
                Debug.LogError("UILevelConfig asset not found. Please create new!");
            }
            else
            {
                Selection.activeObject = configs;
                EditorGUIUtility.PingObject(configs);
            }
        }
    }
}