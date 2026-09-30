/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-22
 */

using Falcon.Modules.UnityLocalization.Runtime;
using UnityEditor;
using UnityEngine;

namespace Falcon.Modules.UnityLocalization.Editor
{
    public class ULocFontCellComponent : IEditorComponent
    {
        private readonly Row _row;

        public ULocFontCellComponent(Row row)
        {
            _row = row;
        }
        
        public void OnGUI()
        {
            var t = _row.text;
            if (!t) return;
            
            var comp = _row.cachedFontComp ?? t.GetComponent<LocalizeTMP_FontComponent>();
            if (comp)
            {
                if (_row.cachedFontComp != comp)
                {
                    _row.cachedFontComp = comp;
                    if (_row.cachedFontSO != null)
                    {
                        _row.cachedFontSO.Dispose();
                        _row.cachedFontSO = null;
                    }
                }
                
                if (_row.cachedFontSO == null || _row.cachedFontSO.targetObject != comp)
                {
                    _row.cachedFontSO?.Dispose();
                    _row.cachedFontSO = new SerializedObject(comp);
                }
                else
                {
                    _row.cachedFontSO.Update();
                }
                
                var sp = _row.cachedFontSO.FindProperty("m_LocalizedAssetReference");
                if (sp != null)
                {
                    EditorGUI.BeginChangeCheck();
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Height(18)); // full rect (label + field)
                    EditorGUI.PropertyField(rect, sp);
                    sp.isExpanded = false;                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        _row.cachedFontSO.ApplyModifiedProperties();
                        EditorUtility.SetDirty(comp);
                    }
                }
                else
                {
                    var rect = GUILayoutUtility.GetRect(100, 18);
                    GUI.Label(rect, "(No LocalizedTMP_Font field found)");
                }
            }
            else
            {
                if (_row.cachedFontComp)
                {
                    _row.cachedFontComp = null;
                    if (_row.cachedFontSO != null)
                    {
                        _row.cachedFontSO.Dispose();
                        _row.cachedFontSO = null;
                    }
                }
                
                if (GUILayout.Button("+ Add Font", GUILayout.Height(18)))
                {
                    t.gameObject.AddLocalizeTMP_FontComp(ULocEditorPrefab.defaultFont);
                    _row.hasFontComp = true;
                    _row.cachedFontComp = t.GetComponent<LocalizeTMP_FontComponent>();
                }
            }
        }
    }
}