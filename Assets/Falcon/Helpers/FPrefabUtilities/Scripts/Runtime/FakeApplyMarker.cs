using UnityEngine;

namespace Falcon.Helpers.FPrefabUtilities.Runtime
{
    /// <summary>
    /// Prefab variant có component này sẽ được batch Fake Apply.
    /// Chỉ là marker, không có logic runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class FakeApplyMarker : MonoBehaviour
    {
        // Empty – marker only
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(FakeApplyMarker))]
    public class AutoFakeApplyComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.HelpBox(
                "Prefab variant có component này sẽ có thể Fake Apply.\nHãy gắn nó vào Prefab base tương ứng.",
                UnityEditor.MessageType.Info
            );
        }
    }
#endif
}
