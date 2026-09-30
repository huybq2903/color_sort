/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-17
*/

namespace Falcon.Helpers.UI
{
    using System.Collections;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;

    [ExecuteAlways]
    public class UITMPCurve : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _text;

        public bool dependOnRectBound = true;
        public AnimationCurve vertexCurve;
        public float yCurveScaling = 100f;

        [Header("Performance")]
        public bool updateOnTextChanged = true;

        private bool _isForceUpdatingMesh;

        private void Reset()
        {
            if (!_text) _text = gameObject.GetComponent<TMP_Text>();

            vertexCurve = new AnimationCurve(new Keyframe(0, 0, 0, 30, 0, 0.01f), new Keyframe(0.5f, 0.25f), new Keyframe(1, 0, -30, 0, 0.01f, 0))
            {
                preWrapMode = WrapMode.Clamp,
                postWrapMode = WrapMode.Clamp
            };

            WarpText();
        }

        private void Awake()
        {
            if (!_text) _text = gameObject.GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ReactToTextChanged);
            
            if (updateOnTextChanged)
            {
                StartCoroutine(UpdateTextCoroutine());
            }
            else
            {
                RefreshText();
            }
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ReactToTextChanged);
            _text.ForceMeshUpdate();
        }

        private IEnumerator UpdateTextCoroutine()
        {
            var numberUpdate = 3;
            for (int i = 0; i < numberUpdate; i++)
            {
                RefreshText();
                yield return new WaitForEndOfFrame();
            }
        }

        [Button]
        public void RefreshText()
        {
            if (_text != null && gameObject.activeInHierarchy)
            {
                WarpText();
            }
        }

        private void ReactToTextChanged(UnityEngine.Object obj)
        {
            TMP_Text tmpText = obj as TMP_Text;
            if (tmpText && _text && tmpText == _text && !_isForceUpdatingMesh) RefreshText();   
        }

        private void WarpText()
        {
            try
            {
                if (!_text) return;
                _isForceUpdatingMesh = true;

                Vector3[] vertices;
                Matrix4x4 matrix;

                _text.havePropertiesChanged = true;
                _text.ForceMeshUpdate();

                TMP_TextInfo textInfo = _text.textInfo;
                if (textInfo == null) return;

                int characterCount = textInfo.characterCount;
                if (characterCount == 0) return;

                float boundsMinX, boundsMaxX;
                if (dependOnRectBound)
                {
                    var textRect = _text.rectTransform.rect;
                    boundsMinX = textRect.min.x;
                    boundsMaxX = textRect.max.x;
                }
                else
                {
                    boundsMinX = _text.bounds.min.x;
                    boundsMaxX = _text.bounds.max.x;
                }

                for (int i = 0; i < characterCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible) continue;

                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                    vertices = textInfo.meshInfo[materialIndex].vertices;

                    Vector3 offsetToMidBaseline = new Vector2(
                        (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2,
                        textInfo.characterInfo[i].baseLine);

                    vertices[vertexIndex + 0] += -offsetToMidBaseline;
                    vertices[vertexIndex + 1] += -offsetToMidBaseline;
                    vertices[vertexIndex + 2] += -offsetToMidBaseline;
                    vertices[vertexIndex + 3] += -offsetToMidBaseline;

                    float x0 = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX);
                    float x1 = x0 + 0.0001f;
                    float y0 = vertexCurve.Evaluate(x0) * yCurveScaling;
                    float y1 = vertexCurve.Evaluate(x1) * yCurveScaling;

                    Vector3 horizontal = new Vector3(1, 0, 0);
                    Vector3 tangent = new Vector3(x1 * (boundsMaxX - boundsMinX) + boundsMinX, y1) -
                                    new Vector3(offsetToMidBaseline.x, y0);

                    float dot = Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * Mathf.Rad2Deg;
                    Vector3 cross = Vector3.Cross(horizontal, tangent);
                    float angle = cross.z > 0 ? dot : 360 - dot;

                    matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

                    vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                    vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                    vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                    vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                    vertices[vertexIndex + 0] += offsetToMidBaseline;
                    vertices[vertexIndex + 1] += offsetToMidBaseline;
                    vertices[vertexIndex + 2] += offsetToMidBaseline;
                    vertices[vertexIndex + 3] += offsetToMidBaseline;
                }

                // Update all vertex data once, not per character
                _text.UpdateVertexData();
                _isForceUpdatingMesh = false;
            }
            catch
            {
                WarpText_Legacy();
                Debug.LogError($"CurveTmp : {gameObject.name} Error! Automatically Backup");
            }
        }

        private void WarpText_Legacy()
        {
            if (!_text) return;
            _isForceUpdatingMesh = true;

            Vector3[] vertices;
            Matrix4x4 matrix;

            _text.havePropertiesChanged = true; // Need to force the TextMeshPro Object to be updated.
            _text.ForceMeshUpdate(); // Generate the mesh and populate the textInfo with data we can use and manipulate.

            TMP_TextInfo textInfo = _text.textInfo;
            if (textInfo == null) return;
            int characterCount = textInfo.characterInfo.Length;

            if (characterCount == 0 || textInfo.characterCount == 0) return;

            float boundsMinX;
            float boundsMaxX;
            if (dependOnRectBound)
            {
                var textRect = _text.rectTransform.rect;
                boundsMinX = textRect.min.x;
                boundsMaxX = textRect.max.x;
            }
            else
            {
                boundsMinX = _text.bounds.min.x;
                boundsMaxX = _text.bounds.max.x;
            }

            for (int i = 0; i < characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible) continue;

                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                // Get the index of the mesh used by this character.
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
                vertices = textInfo.meshInfo[materialIndex].vertices;

                // Compute the baseline mid point for each character
                Vector3 offsetToMidBaseline = new Vector2(
                    (vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, textInfo.characterInfo[i].baseLine);

                // Apply offset to adjust our pivot point.
                vertices[vertexIndex + 0] += -offsetToMidBaseline;
                vertices[vertexIndex + 1] += -offsetToMidBaseline;
                vertices[vertexIndex + 2] += -offsetToMidBaseline;
                vertices[vertexIndex + 3] += -offsetToMidBaseline;

                // Compute the angle of rotation for each character based on the animation curve
                // Character's position relative to the bounds of the mesh.
                float x0 = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX);
                float x1 = x0 + 0.0001f;
                float y0 = vertexCurve.Evaluate(x0) * yCurveScaling;
                float y1 = vertexCurve.Evaluate(x1) * yCurveScaling;

                Vector3 horizontal = new Vector3(1, 0, 0);
                Vector3 tangent = new Vector3(x1 * (boundsMaxX - boundsMinX) + boundsMinX, y1) -
                    new Vector3(offsetToMidBaseline.x, y0);

                float dot = Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * Mathf.Rad2Deg;
                Vector3 cross = Vector3.Cross(horizontal, tangent);
                float angle = cross.z > 0 ? dot : 360 - dot;

                matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

                vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                vertices[vertexIndex + 0] += offsetToMidBaseline;
                vertices[vertexIndex + 1] += offsetToMidBaseline;
                vertices[vertexIndex + 2] += offsetToMidBaseline;
                vertices[vertexIndex + 3] += offsetToMidBaseline;

                // Upload the mesh with the revised information
                _text.UpdateVertexData();
            }

            _isForceUpdatingMesh = false;
        }
    }
}