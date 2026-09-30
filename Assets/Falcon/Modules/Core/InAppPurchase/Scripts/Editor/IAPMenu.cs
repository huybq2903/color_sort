/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using System.IO;
using Falcon.Modules.Core.InAppPurchase.Runtime;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Core.InAppPurchase.Editor
{
    public class IAPMenu : UnityEditor.Editor
    {
        [MenuItem("Falcon/Modules/InApp/Settings")]
        public static void InAppSettings()
        {
            if (!Directory.Exists(IAPConstant.PATH_CONFIG))
            {
                Directory.CreateDirectory(IAPConstant.PATH_CONFIG);
            }
            
            var config = Resources.Load<SOInAppPurchaseConfig>(IAPConstant.NAME_CONFIG);
            if (!config)
            {
                config = CreateInstance<SOInAppPurchaseConfig>();
                AssetDatabase.CreateAsset(config, IAPConstant.PATH_CONFIG + "/" + IAPConstant.NAME_CONFIG + ".asset");
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
            }

            Selection.activeObject = config;
        }
    }
}
