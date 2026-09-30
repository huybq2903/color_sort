/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-16
 */

using Falcon.Helpers.ConfigImporter.Editor;
using UnityEditor;

namespace Falcon.Modules.UI.Purchase.Editor
{
    public class ImportAsset
    {
        [MenuItem("Falcon/Modules/InApp/Import UI Purchase")]
        private static void Import()
        {
            ConfigImporter.ImportConfig("Falcon/Modules/UI/UIPurchase");
        }
    }
}