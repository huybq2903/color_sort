/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-21
 */

using System.IO;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using UnityEditor;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Editor
{
    public class SOAdsSettingInspector : UnityEditor.Editor
    {
        private static readonly string kPathAsset =
            $"Assets/FalconAssets/Modules/Core/ThirdParty/Mediation/Resources/SOAdsSetting.asset";

        private static SOAdsSetting _falconAdsSettings;

        private static SOAdsSetting FalconAdsSettings
        {
            get
            {
                if (_falconAdsSettings != null) return _falconAdsSettings;

                _falconAdsSettings = AssetDatabase.LoadAssetAtPath<SOAdsSetting>(kPathAsset);
                if (_falconAdsSettings != null) return _falconAdsSettings;

                var asset = CreateInstance<SOAdsSetting>();
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

        [MenuItem("Falcon/Modules/ThirdParty/Ads settings", false, 3)]
        public static void MediationSettings()
        {
            var adjustSettings = FalconAdsSettings;
            Selection.activeObject = adjustSettings;
        }
    }
}