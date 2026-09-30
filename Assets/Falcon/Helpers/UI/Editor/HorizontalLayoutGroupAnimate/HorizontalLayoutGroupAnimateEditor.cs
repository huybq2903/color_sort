using UnityEditor;
using UnityEditor.UI;
using Falcon.Helpers.UI;
using UnityEngine.UI;

[CustomEditor(typeof(HorizontalLayoutGroupAnimate), true)]
[CanEditMultipleObjects]
public class HorizontalLayoutGroupAnimateEditor : HorizontalOrVerticalLayoutGroupEditor
{
    private SerializedProperty _animationDuration;
    private SerializedProperty _animationEase;

    protected override void OnEnable()
    {
        base.OnEnable();
        _animationDuration = serializedObject.FindProperty("animationDuration");
        _animationEase = serializedObject.FindProperty("animationEase");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.PropertyField(_animationDuration);
        EditorGUILayout.PropertyField(_animationEase);

        serializedObject.ApplyModifiedProperties();
    }
}