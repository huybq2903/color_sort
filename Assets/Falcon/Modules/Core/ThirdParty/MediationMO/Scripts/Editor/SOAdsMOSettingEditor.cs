/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using Falcon.Modules.Core.ThirdParty.MediationMO.Runtime;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.MediationMO.Editor
{
    [CustomEditor(typeof(SOAdsMOSetting))]
    public class SOAdsMOSettingEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            SirenixEditorGUI.HorizontalLineSeparator();

            var settings = (SOAdsMOSetting)target;

            if (GUILayout.Button("Save"))
            {
                // Debug.LogError(settings.ConfigAds);
            }
        }
    }
}