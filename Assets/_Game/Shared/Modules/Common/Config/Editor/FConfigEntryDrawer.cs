using UnityEditor;
using UnityEngine;

namespace Falcon.Shared.Common.Editor
{
    /// <summary>
    /// Vẽ mỗi entry trên một dòng: [key] [type] [value].
    /// Chỉ field ứng với type đang chọn được hiển thị.
    /// </summary>
    [CustomPropertyDrawer(typeof(FConfigEntry))]
    public class FConfigEntryDrawer : PropertyDrawer
    {
        private const float Spacing = 4f;
        private const float TypeWidth = 70f;
        private const float KeyRatio = 0.42f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative(nameof(FConfigEntry.value));
            var field = ValueField(value);
            return field == null
                ? EditorGUIUtility.singleLineHeight
                : EditorGUI.GetPropertyHeight(field, GUIContent.none, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var key = property.FindPropertyRelative(nameof(FConfigEntry.key));
            var value = property.FindPropertyRelative(nameof(FConfigEntry.value));
            var type = value.FindPropertyRelative(nameof(FConfigValue.type));

            var line = EditorGUIUtility.singleLineHeight;
            var remaining = position.width - TypeWidth - Spacing * 2;
            var keyWidth = remaining * KeyRatio;

            var keyRect = new Rect(position.x, position.y, keyWidth, line);
            var typeRect = new Rect(keyRect.xMax + Spacing, position.y, TypeWidth, line);
            var valueRect = new Rect(typeRect.xMax + Spacing, position.y,
                position.width - keyWidth - TypeWidth - Spacing * 2, position.height);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.PropertyField(keyRect, key, GUIContent.none);
            EditorGUI.PropertyField(typeRect, type, GUIContent.none);

            var field = ValueField(value);
            if (field != null)
                EditorGUI.PropertyField(valueRect, field, GUIContent.none, true);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static SerializedProperty ValueField(SerializedProperty value)
        {
            var type = value.FindPropertyRelative(nameof(FConfigValue.type));
            var selected = (FConfigValueType)type.enumValueIndex;
            return value.FindPropertyRelative(FConfigValue.FieldNameOf(selected));
        }
    }
}
