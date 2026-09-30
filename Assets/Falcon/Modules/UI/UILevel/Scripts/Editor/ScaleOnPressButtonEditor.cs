/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
*/

using Falcon.Modules.UI.Level.Runtime;
using UnityEditor;
using UnityEditor.UI;

namespace Falcon.Modules.UI.Level.Editor
{
    [CustomEditor(typeof(ScaleOnPressButton), true)] 
    [CanEditMultipleObjects]
    public class ScaleOnPressButtonEditor : ButtonEditor
    {
        SerializedProperty _downScaleProp;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _downScaleProp = serializedObject.FindProperty("_scaleFactor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(_downScaleProp);
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}