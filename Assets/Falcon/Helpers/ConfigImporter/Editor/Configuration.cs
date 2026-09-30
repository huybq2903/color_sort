    /*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-07-03
     */


namespace Falcon.Helpers.ConfigImporter.Editor
{
    public static class Configuration
    {
        public const string SETUP_PATH            = @"SetupFile";
        
        public const string URL_MAIN              = @"https://jp-osa-1.linodeobjects.com";
        public const string BUCKET                = @"falcon-framework-libs";
        public const string DIRECTORY             = @"game-lib";
        public const string THIRD_PARTY_DIRECTORY = @"3rd-lib";

        public const string THIRD_PARTY_CONFIG_FILE = @"3rd-config.json";
        
        public const string CONFIG_ASSET_FILE_NAME = @"ModuleAssets.unitypackage";

        public static readonly string kURLPrefix           = $"{URL_MAIN}/{BUCKET}/{DIRECTORY}";
        public static readonly string k3rdURLPrefix        = $"{URL_MAIN}/{BUCKET}/{THIRD_PARTY_DIRECTORY}";
        public static readonly string k3rdRegistryFileLink = $"{URL_MAIN}/{BUCKET}/{THIRD_PARTY_DIRECTORY}/{THIRD_PARTY_CONFIG_FILE}";
    }
}