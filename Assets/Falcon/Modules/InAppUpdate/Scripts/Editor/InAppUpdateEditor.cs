using Falcon.Helpers.ConfigImporter.Editor;
using UnityEditor;

namespace Falcon.Modules.InAppUpdate.Scripts.Editor
{
    public static class InAppUpdateEditor
    {
        [MenuItem("Falcon/Modules/InAppUpdate/Assets")]
        private static void ImportMyModuleConfig()
        {
            var modulePath = "Falcon/Modules/InAppUpdate";
            ConfigImporter.ImportConfig(modulePath);
        }
    }
}
