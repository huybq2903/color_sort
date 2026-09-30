/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-22
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Falcon.Modules.UnityLocalization.Editor
{
    public class ULocStringCellComponent : IEditorComponent
    {
        private readonly Row _row;

        public ULocStringCellComponent(Row row)
        {
            _row = row;
        }

        public void OnGUI()
        {
            var t = _row.text;
            if (!t) return;
            
            var lse = _row.cachedStringComp ?? t.GetComponent<LocalizeStringEvent>();
            if (lse)
            {
                if (_row.cachedStringComp != lse)
                {
                    _row.cachedStringComp = lse;
                    if (_row.cachedStringSO != null)
                    {
                        _row.cachedStringSO.Dispose();
                        _row.cachedStringSO = null;
                    }
                }
                
                if (_row.cachedStringSO == null || _row.cachedStringSO.targetObject != lse)
                {
                    _row.cachedStringSO?.Dispose();
                    _row.cachedStringSO = new SerializedObject(lse);
                }
                else
                {
                    _row.cachedStringSO.Update();
                }
                
                var sp = _row.cachedStringSO.FindProperty("m_StringReference");
                if (sp != null)
                {
                    EditorGUI.BeginChangeCheck();
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
                    EditorGUI.PropertyField(rect, sp);
                    sp.isExpanded = false;
                    if (EditorGUI.EndChangeCheck())
                    {
                        _row.cachedStringSO.ApplyModifiedProperties();
                        EditorUtility.SetDirty(lse);
                    }
                }
            }
            else
            {
                // Clear cache if component was removed
                if (_row.cachedStringComp != null)
                {
                    _row.cachedStringComp = null;
                    if (_row.cachedStringSO != null)
                    {
                        _row.cachedStringSO.Dispose();
                        _row.cachedStringSO = null;
                    }
                }
                
                if (GUILayout.Button("+ Add String", GUILayout.Height(18)))
                {
                    t.gameObject.AddLocalizeStringComp();
                    _row.hasStringComp = true;
                    // Refresh cache after adding
                    _row.cachedStringComp = t.GetComponent<LocalizeStringEvent>();
                }
            }
        }
    }
}