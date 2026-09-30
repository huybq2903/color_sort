/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-3
*/

using UnityEditor;
using Falcon.Helpers.ConfigImporter.Editor;

namespace Falcon.Modules.UI.Settings.Editor
{
    public class ImportModuleAssetsMenu
    {
        [MenuItem("Falcon/Modules/UI/Settings/Assets")]
        private static void ImportAssets()
        {
            var modulePath = "Falcon/Modules/UI/UISettings";
            ConfigImporter.ImportConfig(modulePath);
        }
    }
}
