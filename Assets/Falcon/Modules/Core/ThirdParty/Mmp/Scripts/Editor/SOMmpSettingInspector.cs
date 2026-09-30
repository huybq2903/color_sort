/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using System.IO;
using Falcon.Modules.Core.ThirdParty.Mmp.Runtime;
using UnityEditor;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Editor
{
    public class SOMmpSettingInspector : UnityEditor.Editor
    {
        private static readonly string kPathAsset =
            $"Assets/FalconAssets/Modules/Core/ThirdParty/Mmp/Resources/SOMmpSetting.asset";

        private static SOMmpSetting _falconMmpSettings;

        private static SOMmpSetting FalconMmpSettings
        {
            get
            {
                if (_falconMmpSettings != null) return _falconMmpSettings;

                _falconMmpSettings = AssetDatabase.LoadAssetAtPath<SOMmpSetting>(kPathAsset);
                if (_falconMmpSettings != null) return _falconMmpSettings;

                var asset = CreateInstance<SOMmpSetting>();
                var directoryName = Path.GetDirectoryName(kPathAsset);
                if (directoryName != null)
                {
                    Directory.CreateDirectory(directoryName);
                }

                AssetDatabase.CreateAsset(asset, kPathAsset);
                _falconMmpSettings = asset;
                return _falconMmpSettings;
            }
        }

        [MenuItem("Falcon/Modules/ThirdParty/Mmp settings", false, 3)]
        public static void MmpSettings()
        {
            var adjustSettings = FalconMmpSettings;
            Selection.activeObject = adjustSettings;
        }
    }
}