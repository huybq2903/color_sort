/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-3
*/

using UnityEditor;
using Falcon.Helpers.ConfigImporter.Editor;

namespace Falcon.Modules.UI.Toast.Editor
{
    public class ImportModuleAssetsMenu
    {
        [MenuItem("Falcon/Modules/UI/Toast/Assets")]
        private static void ImportAssets()
        {
            var modulePath = "Falcon/Modules/UI/UIToast";
            ConfigImporter.ImportConfig(modulePath);
        }
    }
}
