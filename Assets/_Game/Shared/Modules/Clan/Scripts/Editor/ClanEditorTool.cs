using UnityEditor;
using UnityEngine;
using Falcon.Helpers.ConfigImporter.Editor;

namespace Game.Shared.Clan.Editor
{
    public class ClanEditorTool
    {
        [MenuItem("Falcon/Shared/Clan/Import Assets")]
        private static void ImportStaminaConfig()
        {
            // Duong dan tuong doi tu Assets, khop vi tri module hien tai
            var path = "_Game/Shared/Modules/Clan";
            ConfigImporter.ImportConfig(path);
        }
    }
}
