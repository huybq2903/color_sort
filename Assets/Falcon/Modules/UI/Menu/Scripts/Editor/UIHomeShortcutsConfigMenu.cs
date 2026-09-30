/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-10
*/

using Falcon.Modules.UI.Menu.Runtime;
using UnityEditor;

namespace Falcon.Modules.UI.Home.Editor
{
    public class UIHomeShortcutsConfigMenu : UnityEditor.Editor
    {
        private const string PATH = @"Assets/FalconAssets/Modules/UI/Menu/Resources";
        private const string NAME = "SO_UI_Home_ShortcutsConfig";

        [MenuItem("Falcon/Modules/UI/*Menu*/UIHome Shortcuts - Config")]
        public static void Config()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UIHomeShortcutsConfig>(PATH + "/" + NAME + ".asset");

            if (settings == null)
            {
                settings = CreateInstance<UIHomeShortcutsConfig>();
                AssetDatabase.CreateAsset(settings, PATH + "/" + NAME + ".asset");
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
            }

            Selection.activeObject = settings;
        }
    }
}
