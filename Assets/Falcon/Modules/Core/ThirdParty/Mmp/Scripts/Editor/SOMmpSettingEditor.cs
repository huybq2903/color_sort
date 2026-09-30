/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using Falcon.Modules.Core.ThirdParty.Mmp.Runtime;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.Core.ThirdParty.Mmp.Editor
{
    [CustomEditor(typeof(SOMmpSetting))]
    public class SOMediationSettingEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            SirenixEditorGUI.HorizontalLineSeparator();

            var settings = (SOMmpSetting)target;

            if (GUILayout.Button("Save"))
            {
                // ValidateEvent(settings);
                // ProcessInstallAdapter(settings);
            }
        }
    }
}