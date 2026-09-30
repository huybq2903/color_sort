/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-16
 */
#if UNITY_EDITOR
namespace Falcon.Shared.FTransform.Editor
{
    using UnityEngine;
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Transform), true)]
    public class TransformEditor : Editor
    {
        public static TransformEditor instance;

        SerializedProperty mPos;
        SerializedProperty mRot;
        SerializedProperty mScale;

        bool mUseWorldSpace = false;
        bool mUniformScale = false;

        void OnEnable()
        {
            instance = this;

            try
            {
                var so = serializedObject;
                mPos = so.FindProperty("m_LocalPosition");
                mRot = so.FindProperty("m_LocalRotation");
                mScale = so.FindProperty("m_LocalScale");
            }
            catch
            {
            }
        }

        void OnDestroy()
        {
            instance = null;
        }

        public override void OnInspectorGUI()
        {
            EditorGUIUtility.labelWidth = 15;
            serializedObject.Update();

            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();

            DrawPosition();
            DrawRotation(false);
            DrawScale(false);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawPosition()
        {
            GUILayout.BeginHorizontal();
            mUseWorldSpace =
                GUILayout.Toggle(mUseWorldSpace, mUseWorldSpace ? "W" : "L", "Button", GUILayout.Width(30f));
            bool reset = GUILayout.Button("P", GUILayout.Width(20f));

            Vector3 pos = Vector3.zero;
            bool mixed = false;

            if (mUseWorldSpace)
            {
                pos = ((Transform)target).position;
            }
            else
            {
                mixed = mPos.hasMultipleDifferentValues;
                pos = mixed ? Vector3.zero : mPos.vector3Value;
            }

            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            float x = EditorGUILayout.FloatField("X", pos.x);
            float y = EditorGUILayout.FloatField("Y", pos.y);
            float z = EditorGUILayout.FloatField("Z", pos.z);
            bool changed = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = false;

            if (reset)
            {
                x = y = z = 0f;
                changed = true;
            }

            if (changed)
            {
                Vector3 newPos = new Vector3(x, y, z);
                Undo.RecordObjects(serializedObject.targetObjects, "Change Position");

                if (mUseWorldSpace)
                {
                    foreach (Object obj in serializedObject.targetObjects)
                        ((Transform)obj).position = newPos;
                }
                else
                {
                    foreach (Object obj in serializedObject.targetObjects)
                        ((Transform)obj).localPosition = newPos;
                }
            }

            GUILayout.EndHorizontal();
        }

        void DrawScale(bool isWidget)
        {
            GUILayout.BeginHorizontal();
            mUniformScale = GUILayout.Toggle(mUniformScale, "U", "Button", GUILayout.Width(30f));
            bool reset = GUILayout.Button("S", GUILayout.Width(20f));

            Vector3 scale = Vector3.one;
            bool mixed = mScale.hasMultipleDifferentValues;

            if (mUseWorldSpace)
            {
                scale = ((Transform)target).lossyScale;
            }
            else
            {
                scale = mixed ? Vector3.one : mScale.vector3Value;
            }

            float x = scale.x, y = scale.y, z = scale.z;

            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();

            x = EditorGUILayout.FloatField("X", x);
            y = EditorGUILayout.FloatField("Y", y);
            z = EditorGUILayout.FloatField("Z", z);

            bool changed = EditorGUI.EndChangeCheck();
            EditorGUI.showMixedValue = false;

            if (reset)
            {
                x = y = z = 1f;
                changed = true;
            }
            else if (mUniformScale)
            {
                const float threshold = 0.0001f;
                bool xChanged = Mathf.Abs(x - scale.x) > threshold;
                bool yChanged = Mathf.Abs(y - scale.y) > threshold;
                bool zChanged = Mathf.Abs(z - scale.z) > threshold;

                int changedCount = (xChanged ? 1 : 0) + (yChanged ? 1 : 0) + (zChanged ? 1 : 0);

                if (changedCount == 1)
                {
                    float newValue = xChanged ? x : yChanged ? y : z;
                    x = y = z = newValue;
                }
            }

            if (changed)
            {
                Vector3 newScale = new Vector3(x, y, z);
                Undo.RecordObjects(serializedObject.targetObjects, "Change Scale");

                if (mUseWorldSpace)
                {
                    foreach (Object obj in serializedObject.targetObjects)
                    {
                        Transform tf = obj as Transform;
                        Vector3 parentScale = tf.parent != null ? tf.parent.lossyScale : Vector3.one;
                        tf.localScale = new Vector3(
                            newScale.x / parentScale.x,
                            newScale.y / parentScale.y,
                            newScale.z / parentScale.z);
                    }
                }
                else
                {
                    foreach (Object obj in serializedObject.targetObjects)
                        ((Transform)obj).localScale = newScale;
                }
            }

            GUILayout.EndHorizontal();
        }

        #region Rotation Handling

        enum Axes : int
        {
            None = 0,
            X = 1,
            Y = 2,
            Z = 4,
            All = 7,
        }

        Axes CheckDifference(Transform t, Vector3 original)
        {
            Vector3 next = t.localEulerAngles;
            Axes axes = Axes.None;
            if (Differs(next.x, original.x)) axes |= Axes.X;
            if (Differs(next.y, original.y)) axes |= Axes.Y;
            if (Differs(next.z, original.z)) axes |= Axes.Z;
            return axes;
        }

        Axes CheckDifference(SerializedProperty property)
        {
            Axes axes = Axes.None;
            if (property.hasMultipleDifferentValues)
            {
                Vector3 original = property.quaternionValue.eulerAngles;
                foreach (Object obj in serializedObject.targetObjects)
                {
                    axes |= CheckDifference(obj as Transform, original);
                    if (axes == Axes.All) break;
                }
            }

            return axes;
        }

        public static bool Differs(float a, float b)
        {
            return Mathf.Abs(a - b) > 0.0001f;
        }

        public static float WrapAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        void DrawRotation(bool isWidget)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(33f);
            bool reset = GUILayout.Button("R", GUILayout.Width(20f));

            Transform t = target as Transform;
            Vector3 visible = mUseWorldSpace ? t.eulerAngles : t.localEulerAngles;

            visible.x = WrapAngle(visible.x);
            visible.y = WrapAngle(visible.y);
            visible.z = WrapAngle(visible.z);

            Axes changed = CheckDifference(mRot);

            EditorGUI.BeginChangeCheck();

            EditorGUI.showMixedValue = (changed & Axes.X) != 0;
            float x = EditorGUILayout.FloatField("X", visible.x);

            EditorGUI.showMixedValue = (changed & Axes.Y) != 0;
            float y = EditorGUILayout.FloatField("Y", visible.y);

            EditorGUI.showMixedValue = (changed & Axes.Z) != 0;
            float z = EditorGUILayout.FloatField("Z", visible.z);

            EditorGUI.showMixedValue = false;

            bool changedByUser = EditorGUI.EndChangeCheck();

            if (reset)
            {
                changedByUser = true;
                x = y = z = 0f;
            }

            if (changedByUser)
            {
                Undo.RecordObjects(serializedObject.targetObjects, "Change Rotation");

                foreach (Object obj in serializedObject.targetObjects)
                {
                    Transform tf = obj as Transform;
                    Vector3 angles = mUseWorldSpace ? tf.eulerAngles : tf.localEulerAngles;

                    angles.x = x;
                    angles.y = y;
                    angles.z = z;

                    if (mUseWorldSpace)
                        tf.eulerAngles = angles;
                    else
                        tf.localEulerAngles = angles;
                }
            }

            GUILayout.EndHorizontal();
        }

        #endregion
    }
}
#endif
