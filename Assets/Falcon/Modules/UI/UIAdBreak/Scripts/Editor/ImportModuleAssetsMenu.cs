/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-3
*/

using UnityEditor;
using Falcon.Helpers.ConfigImporter.Editor;

namespace Falcon.Modules.UI.AdBreak.Editor
{
    public class ImportModuleAssetsMenu
    {
        [MenuItem("Falcon/Modules/UI/AdBreak/Assets")]
        private static void ImportAssets()
        {
            var modulePath = "Falcon/Modules/UI/UIAdBreak";
            ConfigImporter.ImportConfig(modulePath);
        }
    }
}
