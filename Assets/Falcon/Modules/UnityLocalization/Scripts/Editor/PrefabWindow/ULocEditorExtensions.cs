/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-22
 */

using Falcon.Modules.UnityLocalization.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;

namespace Falcon.Modules.UnityLocalization.Editor
{
    public static class ULocEditorExtensions
    {
        public static bool GameObjectHasMissingScripts(this GameObject go)
        {
            var components = go.GetComponents<Component>();
            foreach (var c in components)
                if (!c) return true;
            return false;
        }

        public static void AddLocalizeTMP_FontComp(this GameObject go, LocalizedTMP_Font defaultFont)
        {
            Undo.RegisterCompleteObjectUndo(go, "Add LocalizeTMP_FontComponent");
            var locFont = Undo.AddComponent<LocalizeTMP_FontComponent>(go);
            locFont.AssetReference = defaultFont;
            EditorUtility.SetDirty(go);
        }
        
        public static void AddLocalizeStringComp(this GameObject go)
        {
            Undo.RegisterCompleteObjectUndo(go, "Add LocalizeStringEvent");
            var comp = Undo.AddComponent<LocalizeStringEvent>(go);
            var text = go.GetComponent<TMP_Text>();
            var setStringMethod = text.GetType().GetProperty("text")?.GetSetMethod();
            if (setStringMethod != null)
            {
                var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<string>), text, setStringMethod) as UnityAction<string>;
                UnityEventTools.AddPersistentListener(comp.OnUpdateString, methodDelegate);
            }
            comp.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            EditorUtility.SetDirty(text);
        }
    }
}