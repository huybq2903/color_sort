/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
*/

using Falcon.Modules.UI.Settings.Runtime;
using UnityEditor;

namespace Falcon.Modules.UI.Settings.Editor
{
    public class UISettingConfigMenu : UnityEditor.Editor
    {
        private const string PATH = @"Assets/FalconAssets/Modules/UI/UISetting/Resources";
        private const string NAME = "SO_UI_SettingConfig";

        [MenuItem("Falcon/Modules/UI/Settings/Config")]
        public static void Config()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UISettingConfig>(PATH + "/" + NAME + ".asset");

            if (settings == null)
            {
                settings = CreateInstance<UISettingConfig>();
                AssetDatabase.CreateAsset(settings, PATH + "/" + NAME + ".asset");
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
            }

            Selection.activeObject = settings;
        }
    }
}
