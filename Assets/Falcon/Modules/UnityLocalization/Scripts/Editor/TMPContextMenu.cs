/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-19
 */

using Falcon.Modules.UnityLocalization.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace Falcon.Modules.UnityLocalization.Editor
{
    public static class TMPContextMenu
    {
        [MenuItem("CONTEXT/TextMeshProUGUI/Localize & Font")]
        private static void Localize(MenuCommand command)
        {
            var target = command.context as TextMeshProUGUI;
            LocalizationSettings.SelectedLocale = null;
            SetupString(target);
            SetupFont(target);
        }

        private static void SetupString(TextMeshProUGUI target)
        {
            var comp = Undo.AddComponent(target.gameObject, typeof(LocalizeStringEvent)) as LocalizeStringEvent;
            var setStringMethod = target.GetType().GetProperty("text").GetSetMethod();
            var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, setStringMethod) as UnityAction<string>;
            UnityEventTools.AddPersistentListener(comp.OnUpdateString, methodDelegate);
            comp.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
        }

        private static void SetupFont(TextMeshProUGUI target)
        {
            Undo.AddComponent(target.gameObject, typeof(LocalizeTMP_FontComponent));
        }
    }
}