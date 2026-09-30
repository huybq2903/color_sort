/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System.IO;
using Falcon.Helpers.Devkit;
using UnityEditor;

namespace Falcon.Modules.CDN
{
    public class CdnSettingService : Editor
    {
        private static readonly string AssetPath = Path.Combine("Assets", "FalconAssets", "Modules", "Cdn", "Resources",
            "SOCdnSettings.asset");

        private static readonly LazyVal<SOCdnSettings> _cdnSettings = new(() =>
        {
            var setting = AssetDatabase.LoadAssetAtPath<SOCdnSettings>(AssetPath);
            if (setting is not null) return setting;

            var asset = CreateInstance<SOCdnSettings>();
            var directoryName = Path.GetDirectoryName(AssetPath);
            if (directoryName != null) Directory.CreateDirectory(directoryName);

            AssetDatabase.CreateAsset(asset, AssetPath);
            return asset;
        });

        public static SOCdnSettings CdnSettings => _cdnSettings.Value;
    }
}