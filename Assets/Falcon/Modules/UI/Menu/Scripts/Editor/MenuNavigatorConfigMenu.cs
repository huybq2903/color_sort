/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
*/

using Falcon.Modules.UI.Menu.Runtime;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.UI.Menu.Editor
{
    public class MenuNavigatorConfigMenu : UnityEditor.Editor
    {
        private const string PATH = @"Assets/FalconAssets/Modules/UI/Menu/Resources";
        private const string NAME = "SO_UI_Menu_NavigatorConfig";

        [MenuItem("Falcon/Modules/UI/*Menu*/Menu Navigator - Config")]
        public static void Config()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MenuNavigatorConfig>(PATH + "/" + NAME + ".asset");

            if (settings == null)
            {
                settings = CreateInstance<MenuNavigatorConfig>();
                AssetDatabase.CreateAsset(settings, PATH + "/" + NAME + ".asset");
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
            }

            Selection.activeObject = settings;
        }
    }
}
