using Falcon.Shared.Common;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Falcon.Shared.Common
{
    [AddComponentMenu("UI/Linear Layout Fitter")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class LinearLayoutFitter : MonoBehaviour
    {
        public enum LayoutDirection
        {
            Horizontal,
            Vertical
        }

        [SerializeField] private LayoutDirection direction = LayoutDirection.Vertical;
        [SerializeField] private float spacing;
        [SerializeField] private RectOffset padding = new RectOffset();
        [SerializeField] private TextAnchor childAlignment = TextAnchor.UpperLeft;
        [SerializeField] private bool controlChildWidth = true;
        [SerializeField] private bool controlChildHeight = false;
        [SerializeField] private bool childForceExpandWidth = true;
        [SerializeField] private bool childForceExpandHeight = false;
        [SerializeField] private bool fitWidthToChildren;
        [SerializeField] private bool fitHeightToChildren = true;

        private readonly List<RectTransform> _children = new List<RectTransform>();
        private RectTransform _rectTransform;

        private RectTransform RectTransform => _rectTransform != null
            ? _rectTransform
            : _rectTransform = (RectTransform)transform;
        
        public void ApplyLayoutImmediate()
        {
            Canvas.ForceUpdateCanvases();
            CollectChildren(_children);

            for (int i = 0; i < _children.Count; i++)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_children[i]);

            if (fitWidthToChildren)
                FitOwnAxis(_children, axis: 0);

            if (fitHeightToChildren)
                FitOwnAxis(_children, axis: 1);

            SetChildrenAlongAxis(_children, axis: 0);
            SetChildrenAlongAxis(_children, axis: 1);
        }

        private void CollectChildren(List<RectTransform> children)
        {
            children.Clear();

            for (int i = 0; i < RectTransform.childCount; i++)
            {
                if (RectTransform.GetChild(i) is not RectTransform child)
                    continue;

                if (!child.gameObject.activeInHierarchy || ShouldIgnoreLayout(child))
                    continue;

                children.Add(child);
            }
        }

        private static bool ShouldIgnoreLayout(RectTransform child)
        {
            var ignorerComponents = ListPool<ILayoutIgnorer>.Get();
            child.GetComponents(ignorerComponents);

            bool shouldIgnore = false;
            for (int i = 0; i < ignorerComponents.Count; i++)
            {
                if (!ignorerComponents[i].ignoreLayout)
                    continue;

                shouldIgnore = true;
                break;
            }

            ListPool<ILayoutIgnorer>.Release(ignorerComponents);
            return shouldIgnore;
        }

        private void SetChildrenAlongAxis(List<RectTransform> children, int axis)
        {
            bool isPrimaryAxis = axis == GetPrimaryAxis();
            float size = RectTransform.rect.size[axis];
            float innerSize = size - GetPaddingSize(axis);

            if (isPrimaryAxis)
            {
                float pos = GetStartOffset(axis, GetContentSpace(children, axis));

                for (int i = 0; i < children.Count; i++)
                {
                    RectTransform child = children[i];
                    float childSize = GetChildSize(child, axis, innerSize);
                    SetChildAlongAxisDirect(child, axis, pos, childSize);
                    pos += childSize + spacing;
                }

                return;
            }

            for (int i = 0; i < children.Count; i++)
            {
                RectTransform child = children[i];
                float childSize = GetChildSize(child, axis, innerSize);
                float startOffset = GetStartOffset(axis, childSize);
                SetChildAlongAxisDirect(child, axis, startOffset, childSize);
            }
        }

        private void FitOwnAxis(List<RectTransform> children, int axis)
        {
            RectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, GetRequiredSpace(children, axis));
        }

        private float GetRequiredSpace(List<RectTransform> children, int axis)
        {
            float total = GetPaddingSize(axis);
            bool isPrimaryAxis = axis == GetPrimaryAxis();

            if (children.Count == 0)
                return total;

            if (isPrimaryAxis)
                total += spacing * (children.Count - 1);

            if (isPrimaryAxis)
            {
                for (int i = 0; i < children.Count; i++)
                    total += GetPreferredChildSize(children[i], axis);

                return total;
            }

            for (int i = 0; i < children.Count; i++)
                total = Mathf.Max(total, GetPreferredChildSize(children[i], axis) + GetPaddingSize(axis));

            return total;
        }

        private float GetContentSpace(List<RectTransform> children, int axis)
        {
            float total = 0f;
            bool isPrimaryAxis = axis == GetPrimaryAxis();

            if (children.Count == 0)
                return total;

            if (isPrimaryAxis)
                total += spacing * (children.Count - 1);

            if (isPrimaryAxis)
            {
                for (int i = 0; i < children.Count; i++)
                    total += GetChildSize(children[i], axis, RectTransform.rect.size[axis] - GetPaddingSize(axis));

                return total;
            }

            for (int i = 0; i < children.Count; i++)
                total = Mathf.Max(total, GetChildSize(children[i], axis, RectTransform.rect.size[axis] - GetPaddingSize(axis)));

            return total;
        }

        private int GetPrimaryAxis()
        {
            return direction == LayoutDirection.Horizontal ? 0 : 1;
        }

        private float GetChildSize(RectTransform child, int axis, float innerSize)
        {
            bool shouldControlSize = axis == 0 ? controlChildWidth : controlChildHeight;
            bool shouldForceExpand = axis == 0 ? childForceExpandWidth : childForceExpandHeight;

            if (!shouldControlSize)
                return child.rect.size[axis];

            float preferred = LayoutUtility.GetPreferredSize(child, axis);
            if (shouldForceExpand)
                return Mathf.Max(preferred, innerSize);

            return preferred;
        }

        private float GetPreferredChildSize(RectTransform child, int axis)
        {
            bool shouldControlSize = axis == 0 ? controlChildWidth : controlChildHeight;
            return shouldControlSize ? LayoutUtility.GetPreferredSize(child, axis) : child.rect.size[axis];
        }

        private float GetStartOffset(int axis, float requiredSpaceWithoutPadding)
        {
            float totalRequiredSpace = requiredSpaceWithoutPadding + GetPaddingSize(axis);
            float availableSpace = RectTransform.rect.size[axis];
            float surplusSpace = availableSpace - totalRequiredSpace;
            return GetLeadingPadding(axis) + surplusSpace * GetAlignmentOnAxis(axis);
        }

        private float GetAlignmentOnAxis(int axis)
        {
            return axis == 0
                ? (int)childAlignment % 3 * 0.5f
                : (float)childAlignment / 3 * 0.5f;
        }

        private float GetPaddingSize(int axis)
        {
            return axis == 0 ? padding.horizontal : padding.vertical;
        }

        private float GetLeadingPadding(int axis)
        {
            return axis == 0 ? padding.left : padding.top;
        }

        private static void SetChildAlongAxisDirect(RectTransform child, int axis, float pos, float size)
        {
            Vector2 anchor = Vector2.up;
            child.anchorMin = anchor;
            child.anchorMax = anchor;

            Vector2 sizeDelta = child.sizeDelta;
            sizeDelta[axis] = size;
            child.sizeDelta = sizeDelta;

            Vector2 anchoredPosition = child.anchoredPosition;
            if (axis == 0)
                anchoredPosition.x = pos + size * child.pivot.x;
            else
                anchoredPosition.y = -pos - size * (1f - child.pivot.y);

            child.anchoredPosition = anchoredPosition;
        }
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(LinearLayoutFitter))]
[CanEditMultipleObjects]
public class LinearLayoutFitterEditor : Editor
{
    private SerializedProperty direction;
    private SerializedProperty spacing;
    private SerializedProperty padding;
    private SerializedProperty childAlignment;
    private SerializedProperty controlChildWidth;
    private SerializedProperty controlChildHeight;
    private SerializedProperty childForceExpandWidth;
    private SerializedProperty childForceExpandHeight;
    private SerializedProperty fitWidthToChildren;
    private SerializedProperty fitHeightToChildren;

    private void OnEnable()
    {
        direction = serializedObject.FindProperty("direction");
        spacing = serializedObject.FindProperty("spacing");
        padding = serializedObject.FindProperty("padding");
        childAlignment = serializedObject.FindProperty("childAlignment");
        controlChildWidth = serializedObject.FindProperty("controlChildWidth");
        controlChildHeight = serializedObject.FindProperty("controlChildHeight");
        childForceExpandWidth = serializedObject.FindProperty("childForceExpandWidth");
        childForceExpandHeight = serializedObject.FindProperty("childForceExpandHeight");
        fitWidthToChildren = serializedObject.FindProperty("fitWidthToChildren");
        fitHeightToChildren = serializedObject.FindProperty("fitHeightToChildren");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(direction, true);
        EditorGUILayout.PropertyField(padding, true);
        EditorGUILayout.PropertyField(spacing, true);
        EditorGUILayout.PropertyField(childAlignment, true);

        DrawTogglePair("Control Child Size", controlChildWidth, controlChildHeight);
        DrawTogglePair("Child Force Expand", childForceExpandWidth, childForceExpandHeight);
        DrawTogglePair("Fit To Children", fitWidthToChildren, fitHeightToChildren);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Apply Layout"))
            (target as LinearLayoutFitter)?.ApplyLayoutImmediate();
    }

    private static void DrawTogglePair(string label, SerializedProperty widthProperty, SerializedProperty heightProperty)
    {
        Rect rect = EditorGUILayout.GetControlRect();
        rect = EditorGUI.PrefixLabel(rect, -1, EditorGUIUtility.TrTextContent(label));
        rect.width = Mathf.Max(50f, (rect.width - 4f) / 2f);

        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 50f;
        ToggleLeft(rect, widthProperty, EditorGUIUtility.TrTextContent("Width"));
        rect.x += rect.width + 2f;
        ToggleLeft(rect, heightProperty, EditorGUIUtility.TrTextContent("Height"));
        EditorGUIUtility.labelWidth = oldLabelWidth;
    }

    private static void ToggleLeft(Rect position, SerializedProperty property, GUIContent label)
    {
        bool toggle = property.boolValue;
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        toggle = EditorGUI.ToggleLeft(position, label, toggle);
        EditorGUI.indentLevel = oldIndent;
        if (EditorGUI.EndChangeCheck())
            property.boolValue = property.hasMultipleDifferentValues ? true : !property.boolValue;

        EditorGUI.EndProperty();
    }
}
#endif
