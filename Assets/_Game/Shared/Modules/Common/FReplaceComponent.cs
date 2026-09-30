/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-13
 */
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Falcon.Shared.Common.Editor
{
    public static class FReplaceComponent
    {
        // ===== Entry: Context menu on any MonoBehaviour =====
        [MenuItem("CONTEXT/MonoBehaviour/Replace With…", false, 1510)]
        private static void ShowReplaceMenu(MenuCommand command)
        {
            var src = command.context as MonoBehaviour;
            if (src == null) return;

            var baseType = src.GetType();
            var derived = GetDerivedTypes(baseType).ToList();
            var win = EditorWindow.focusedWindow;
            var wp = win.position;
            var center = new Vector2(wp.width * 0.5f, wp.height * 0.5f);
            var rect = new Rect(center.x, center.y, 0, 0);

            if (derived.Count == 0)
            {
                EditorUtility.DisplayDialog("Replace With…", $"Không tìm thấy lớp kế thừa của {baseType.Name}.", "OK");
                return;
            }

            var menu = new GenericMenu();
            foreach (var t in derived)
            {
                var captured = t; // tránh closure bug
                var display = $"{captured.Name}";
                menu.AddItem(new GUIContent(display), false, () => ReplaceSingle(src, captured));
            }

            menu.DropDown(rect);
        }

        // ===== Validator =====
        [MenuItem("CONTEXT/MonoBehaviour/Replace With…", true)]
        private static bool ShowReplaceMenu_Validate(MenuCommand command)
        {
            return command.context is MonoBehaviour mb && GetDerivedTypes(mb.GetType()).Any();
        }

        // ===== Core: Replace one component =====
        public static void ReplaceSingle(MonoBehaviour source, Type targetType)
        {
            if (source == null || targetType == null) return;
            var go = source.gameObject;
            var baseType = source.GetType();

            if (!baseType.IsAssignableFrom(targetType) || baseType == targetType)
            {
                Debug.LogWarning($"Target type {targetType.Name} không hợp lệ để thay cho {baseType.Name}.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(go, "Replace Component");
            var index = GetComponentIndex(go, source);

            // Create new component
            Component newComp = Undo.AddComponent(go, targetType);

            // Copy serialized data (except m_Script)
            var srcSO = new SerializedObject(source);
            var dstSO = new SerializedObject(newComp);
            CopySerializedValues(srcSO, dstSO);

            // Keep component order
            MoveToIndex(newComp, index);

            // Keep prefab overrides
            PrefabUtility.RecordPrefabInstancePropertyModifications(go);

            // Destroy old
            Undo.DestroyObjectImmediate(source);

            EditorUtility.SetDirty(go);
            Debug.Log($"Replaced {baseType.Name} → {targetType.Name} on {go.name}.");
        }

        // ===== Copy routine: all serialized props except m_Script =====
        public static void CopySerializedValues(SerializedObject src, SerializedObject dst)
        {
            src.Update();
            dst.Update();

            var prop = src.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.propertyPath == "m_Script") continue; // bỏ qua script ref

                var dstProp = dst.FindProperty(prop.propertyPath);
                if (dstProp == null || dstProp.propertyType != prop.propertyType)
                    continue;

                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        dstProp.intValue = prop.intValue;
                        break;
                    case SerializedPropertyType.Boolean:
                        dstProp.boolValue = prop.boolValue;
                        break;
                    case SerializedPropertyType.Float:
                        dstProp.floatValue = prop.floatValue;
                        break;
                    case SerializedPropertyType.String:
                        dstProp.stringValue = prop.stringValue;
                        break;
                    case SerializedPropertyType.Color:
                        dstProp.colorValue = prop.colorValue;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        dstProp.objectReferenceValue = prop.objectReferenceValue;
                        break;
                    case SerializedPropertyType.LayerMask:
                        dstProp.intValue = prop.intValue;
                        break;
                    case SerializedPropertyType.Enum:
                        dstProp.enumValueIndex = prop.enumValueIndex;
                        break;
                    case SerializedPropertyType.Vector2:
                        dstProp.vector2Value = prop.vector2Value;
                        break;
                    case SerializedPropertyType.Vector3:
                        dstProp.vector3Value = prop.vector3Value;
                        break;
                    case SerializedPropertyType.Vector4:
                        dstProp.vector4Value = prop.vector4Value;
                        break;
                    case SerializedPropertyType.Rect:
                        dstProp.rectValue = prop.rectValue;
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        dstProp.animationCurveValue = prop.animationCurveValue;
                        break;
                    case SerializedPropertyType.Bounds:
                        dstProp.boundsValue = prop.boundsValue;
                        break;
                    case SerializedPropertyType.Quaternion:
                        dstProp.quaternionValue = prop.quaternionValue;
                        break;
                    case SerializedPropertyType.Vector2Int:
                        dstProp.vector2IntValue = prop.vector2IntValue;
                        break;
                    case SerializedPropertyType.Vector3Int:
                        dstProp.vector3IntValue = prop.vector3IntValue;
                        break;
                    case SerializedPropertyType.RectInt:
                        dstProp.rectIntValue = prop.rectIntValue;
                        break;
                    case SerializedPropertyType.BoundsInt:
                        dstProp.boundsIntValue = prop.boundsIntValue;
                        break;
                    default:
                        // Unsupported or complex types (arrays, structs) có thể đệ quy hoặc bỏ qua
                        if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
                        {
                            CopyArray(prop, dstProp);
                        }

                        break;
                }
            }

            dst.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CopyArray(SerializedProperty src, SerializedProperty dst)
        {
            if (!src.isArray || !dst.isArray) return;
            dst.arraySize = src.arraySize;
            for (int i = 0; i < src.arraySize; i++)
            {
                var srcElem = src.GetArrayElementAtIndex(i);
                var dstElem = dst.GetArrayElementAtIndex(i);
                if (srcElem == null || dstElem == null) continue;

                switch (srcElem.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        dstElem.intValue = srcElem.intValue;
                        break;
                    case SerializedPropertyType.Boolean:
                        dstElem.boolValue = srcElem.boolValue;
                        break;
                    case SerializedPropertyType.Float:
                        dstElem.floatValue = srcElem.floatValue;
                        break;
                    case SerializedPropertyType.String:
                        dstElem.stringValue = srcElem.stringValue;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        dstElem.objectReferenceValue = srcElem.objectReferenceValue;
                        break;
                    default:
                        break;
                }
            }
        }

        // ===== Helpers =====
        private static int GetComponentIndex(GameObject go, Component comp)
        {
            var arr = go.GetComponents<Component>();
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == comp)
                    return i;
            return -1;
        }

        private static void MoveToIndex(Component comp, int targetIndex)
        {
            if (targetIndex < 0) return;
            var arr = comp.gameObject.GetComponents<Component>();
            int current = Array.IndexOf(arr, comp);
            if (current < 0) return;

            // ComponentUtility only supports MoveUp; do a small loop.
            while (current > targetIndex)
            {
                ComponentUtility.MoveComponentUp(comp);
                current--;
            }
        }

        public static IEnumerable<Type> GetDerivedTypes(Type baseType)
        {
            // TypeCache is fast and editor-only
            var list = TypeCache.GetTypesDerivedFrom(baseType);
            foreach (var t in list)
            {
                if (t.IsAbstract) continue;
                if (t.IsGenericType) continue;
                if (t.IsDefined(typeof(ObsoleteAttribute), true)) continue;
                // Ensure it's a MonoBehaviour (most common case)
                if (!typeof(MonoBehaviour).IsAssignableFrom(t)) continue;
                yield return t;
            }
        }

        // Utility to fetch expected type of a SerializedProperty (best-effort)
        private static Type GetFieldOrPropertyType(Type host, string propertyPath)
        {
            // Best-effort: only supports simple top-level fields/properties
            try
            {
                var dot = propertyPath.IndexOf('.');
                var name = dot >= 0 ? propertyPath.Substring(0, dot) : propertyPath;

                var f = host.GetField(name,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (f != null) return f.FieldType;

                var p = host.GetProperty(name,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (p != null) return p.PropertyType;
            }
            catch
            {
            }

            return null;
        }
    }
}
#endif