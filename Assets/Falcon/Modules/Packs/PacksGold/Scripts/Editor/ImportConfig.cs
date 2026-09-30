/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-03
 */

using Falcon.Helpers.ConfigImporter.Editor;
using UnityEditor;

namespace Falcon.Modules.Packs.PacksGold.Editor
{
    public static class ImportConfig
    {
        [MenuItem("Falcon/Modules/Pack/Import Pack Gold Asset")]
        private static void ImportAsset()
        {
            ConfigImporter.ImportConfig("Falcon/Modules/Packs/PacksGold");
        }
    }
}