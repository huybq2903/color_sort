/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-03
 */

using Falcon.Helpers.ConfigImporter.Editor;
using UnityEditor;

namespace Falcon.Modules.Packs.PacksFailOffer.Editor
{
    public static class ImportConfig
    {
        [MenuItem("Falcon/Modules/Pack/Import Pack Fail Offer Asset")]
        private static void ImportAsset()
        {
            ConfigImporter.ImportConfig("Falcon/Modules/Packs/PacksFailOffer");
        }
    }
}