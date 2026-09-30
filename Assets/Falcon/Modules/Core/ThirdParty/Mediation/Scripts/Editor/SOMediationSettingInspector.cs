/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using System.IO;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using UnityEditor;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Editor
{
    public class SOMediationSettingInspector : UnityEditor.Editor
    {
        private static readonly string kPathAsset =
            $"Assets/FalconAssets/Modules/Core/ThirdParty/Mediation/Resources/SOMediationSetting.asset";

        private static SOMediationSetting _falconMediationSettings;

        private static SOMediationSetting FalconMediationSettings
        {
            get
            {
                if (_falconMediationSettings != null) return _falconMediationSettings;

                _falconMediationSettings = AssetDatabase.LoadAssetAtPath<SOMediationSetting>(kPathAsset);
                if (_falconMediationSettings != null) return _falconMediationSettings;

                var asset = CreateInstance<SOMediationSetting>();
                var directoryName = Path.GetDirectoryName(kPathAsset);
                if (directoryName != null)
                {
                    Directory.CreateDirectory(directoryName);
                }

                AssetDatabase.CreateAsset(asset, kPathAsset);
                _falconMediationSettings = asset;
                return _falconMediationSettings;
            }
        }

        [MenuItem("Falcon/Modules/ThirdParty/Mediation settings", false, 3)]
        public static void MediationSettings()
        {
            var adjustSettings = FalconMediationSettings;
            Selection.activeObject = adjustSettings;
        }
    }
}