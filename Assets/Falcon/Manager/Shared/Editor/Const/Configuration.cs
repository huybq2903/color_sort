/*
 * Author: ngocdx
 * Email: ngocdx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Shared
{
    public static class Configuration
    {
        public const string PATH_MODULES              = @"Falcon/Modules";
        public const string PATH_PREFIX_TO_EXPORT     = @"FalconAssets/Modules";
        public const string PATH_PREFIX_3RD_TO_EXPORT = @"FalconAssets/ThirdPartySetupFile";
        public const string SETUP_PATH                = @"SetupFile";
        
        public const string URL_MAIN              = @"https://jp-osa-1.linodeobjects.com";
        public const string BUCKET                = @"falcon-framework-libs";
        public const string DIRECTORY             = @"game-lib";
        public const string THIRD_PARTY_DIRECTORY = @"3rd-lib";
        
        public const string EXPORT_PATH  = @"Assets/Falcon/Manager/Exporter/Gallery";
        
        public const string REGISTRY_FILE_NAME     = @"central-registry.json";
        public const string PACKAGE_CONFIG_FILE    = @"package.json";
        public const string CONFIG_ASSET_FILE_NAME = @"ModuleAssets.unitypackage";

        public const string DOCUMENT_URL  = @"https://falcon-game-studio.gitbook.io/falcon-unity-frameworks/";
        public const string CMS_SEVER_URL = @"https://puzzle.data4game.com/api/v1/";
        public const string CMS_WEB_URL   = @"http://puzzle.data4game.com";

        public const string GAME_TEMPLATE_CDN_BASE = "https://jp-osa-1.linodeobjects.com/falcon-framework-libs/game-templates";

        public static readonly string kURLPrefix = $"{URL_MAIN}/{BUCKET}/{DIRECTORY}";
        public static readonly string k3rdURLPrefix     = $"{URL_MAIN}/{BUCKET}/{THIRD_PARTY_DIRECTORY}";
        public static readonly string kRegistryFileLink = $"{URL_MAIN}/{BUCKET}/{DIRECTORY}/{REGISTRY_FILE_NAME}";
    }
}