/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using System.IO;
using Falcon.Modules.Core.ThirdParty.MediationMO.Runtime;
using UnityEditor;

namespace Falcon.Modules.Core.ThirdParty.MediationMO.Editor
{
    public class SOAdsMOSettingInspector : UnityEditor.Editor
    {
        private static readonly string kPathAsset =
            $"Assets/FalconAssets/Modules/Core/ThirdParty/MediationMO/Resources/SOAdsMOV3Setting.asset";

        private static SOAdsMOSetting _falconAdsSettings;

        private static SOAdsMOSetting FalconAdsSettings
        {
            get
            {
                if (_falconAdsSettings != null) return _falconAdsSettings;

                _falconAdsSettings = AssetDatabase.LoadAssetAtPath<SOAdsMOSetting>(kPathAsset);
                if (_falconAdsSettings != null) return _falconAdsSettings;

                var asset = CreateInstance<SOAdsMOSetting>();
                var directoryName = Path.GetDirectoryName(kPathAsset);
                if (directoryName != null)
                {
                    Directory.CreateDirectory(directoryName);
                }

                AssetDatabase.CreateAsset(asset, kPathAsset);
                _falconAdsSettings = asset;
                return _falconAdsSettings;
            }
        }

        [MenuItem("Falcon/Modules/ThirdParty/Ads MO settings", false, 3)]
        public static void MediationSettings()
        {
            var adjustSettings = FalconAdsSettings;
            Selection.activeObject = adjustSettings;
        }
    }
}