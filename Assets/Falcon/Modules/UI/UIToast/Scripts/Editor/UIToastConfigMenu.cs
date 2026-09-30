/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
*/

using Falcon.Modules.UI.Toast.Runtime;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.UI.Toast.Editor
{
    public class UIToastConfigMenu : UnityEditor.Editor
    {
        private const string PATH = @"Assets/FalconAssets/Modules/UI/UIToast/Resources";
        private const string NAME = "SO_UI_ToastConfig";

        [MenuItem("Falcon/Modules/UI/Toast/Config")]
        public static void Config()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UIToastConfig>(PATH + "/" + NAME + ".asset");

            if (settings == null)
            {
                settings = CreateInstance<UIToastConfig>();
                AssetDatabase.CreateAsset(settings, PATH + "/" + NAME + ".asset");
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
            }

            Selection.activeObject = settings;
        }
    }
}
