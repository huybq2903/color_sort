/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-05-13
*/

namespace Falcon.Helpers.UI
{
    using System.Collections;
    using Sirenix.OdinInspector;
    using TMPro;
    using UnityEngine;

    [ExecuteAlways]
    public class UITMPCurveTween : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _text;

        public bool dependOnRectBound = true;
        public AnimationCurve vertexCurve;
        public float yCurveScaling = 100f;

        [Header("Performance")]
        public bool updateOnTextChanged = true;

        [Header("Tween Scale")]
        public bool playOnEnable = true;
        public float startDelay = 0.125f;
        public float scaleFrom = 0f;
        public float scaleTo = 1f;
        public float scaleDuration = 0.3f;
        public float delayBetweenChars = 0.05f;
        public AnimationCurve scaleEaseCurve;

        private bool _isForceUpdatingMesh;
        private float[] _charScales;
        private Coroutine _scaleCoroutine;

        private void Reset()
        {
            if (!_text) _text = gameObject.GetComponent<TMP_Text>();

            vertexCurve = new AnimationCurve(new Keyframe(0, 0, 0, 30, 0, 0.01f), new Keyframe(0.5f, 0.25f), new Keyframe(1, 0, -30, 0, 0.01f, 0))
            {
                preWrapMode = WrapMode.Clamp,
                postWrapMode = WrapMode.Clamp
            };

            scaleEaseCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.7f, 1.2f), new Keyframe(1, 1))
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

            StartCoroutine(IEOnEnableCoroutine());
            IEnumerator IEOnEnableCoroutine()
            {
                if (playOnEnable && _text != null)
                {
                    _isForceUpdatingMesh = true;
                    _text.havePropertiesChanged = true;
                    _text.ForceMeshUpdate();
                    int count = _text.textInfo.characterCount;
                    _isForceUpdatingMesh = false;
                    if (count > 0)
                    {
                        _charScales = new float[count];
                        for (int i = 0; i < count; i++) _charScales[i] = scaleFrom;
                    }
                }

                if (updateOnTextChanged)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        RefreshText();
                        yield return new WaitForEndOfFrame();
                    }
                }
                else
                {
                    RefreshText();
                }

                if (playOnEnable)
                {
                    PlayScaleAnimation();
                }
            }
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ReactToTextChanged);
            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
                _scaleCoroutine = null;
            }
            if (_text) _text.ForceMeshUpdate();
        }

        [Button]
        public void RefreshText()
        {
            if (_text != null && gameObject.activeInHierarchy)
            {
                WarpText();
            }
        }

        [Button]
        public void PlayScaleAnimation()
        {
            if (_text == null || !gameObject.activeInHierarchy) return;

            _text.havePropertiesChanged = true;
            _text.ForceMeshUpdate();
            int charCount = _text.textInfo.characterCount;
            if (charCount == 0) return;

            _charScales = new float[charCount];
            for (int i = 0; i < charCount; i++) _charScales[i] = scaleFrom;
            WarpText();

            // Re-sync sau WarpText vì ForceMeshUpdate bên trong có thể trả về count khác
            int syncedCount = _text.textInfo.characterCount;
            if (syncedCount != charCount)
            {
                var prev = _charScales;
                _charScales = new float[syncedCount];
                for (int i = 0; i < syncedCount; i++)
                    _charScales[i] = i < prev.Length ? prev[i] : scaleFrom;
                charCount = syncedCount;
            }

            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleAnimCoroutine(charCount));
        }

        private IEnumerator ScaleAnimCoroutine(int charCount)
        {
            if (startDelay > 0f)
            {
                float delayElapsed = 0f;
                while (delayElapsed < startDelay)
                {
                    delayElapsed += Time.deltaTime;
                    yield return null;
                }
            }

            float totalDuration = (charCount - 1) * delayBetweenChars + scaleDuration;
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                for (int i = 0; i < charCount; i++)
                {
                    float charElapsed = elapsed - i * delayBetweenChars;
                    float t = Mathf.Clamp01(charElapsed / scaleDuration);
                    float easedT = (scaleEaseCurve != null && scaleEaseCurve.length > 0)
                        ? scaleEaseCurve.Evaluate(t)
                        : t;
                    _charScales[i] = Mathf.Lerp(scaleFrom, scaleTo, easedT);
                }
                WarpText();
                elapsed += Time.deltaTime;
                yield return null;
            }

            float endEasedT = (scaleEaseCurve != null && scaleEaseCurve.length > 0)
                ? scaleEaseCurve.Evaluate(1f)
                : 1f;
            float endScale = Mathf.Lerp(scaleFrom, scaleTo, endEasedT);
            for (int i = 0; i < charCount; i++) _charScales[i] = endScale;

            WarpText();
            _scaleCoroutine = null;
        }

        private void ReactToTextChanged(UnityEngine.Object obj)
        {
            TMP_Text tmpText = obj as TMP_Text;
            if (tmpText && _text && tmpText == _text && !_isForceUpdatingMesh) RefreshText();
        }

        private float GetCharScale(int index)
        {
            if (_charScales == null || index >= _charScales.Length)
                return _scaleCoroutine != null || playOnEnable ? scaleFrom : 1f;
            return _charScales[index];
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
                if (textInfo == null) { _isForceUpdatingMesh = false; return; }

                int characterCount = textInfo.characterCount;
                if (characterCount == 0) { _isForceUpdatingMesh = false; return; }

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

                    float charScale = GetCharScale(i);
                    matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one * charScale);

                    vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                    vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                    vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                    vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                    vertices[vertexIndex + 0] += offsetToMidBaseline;
                    vertices[vertexIndex + 1] += offsetToMidBaseline;
                    vertices[vertexIndex + 2] += offsetToMidBaseline;
                    vertices[vertexIndex + 3] += offsetToMidBaseline;
                }

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

                matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one * GetCharScale(i));

                vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                vertices[vertexIndex + 0] += offsetToMidBaseline;
                vertices[vertexIndex + 1] += offsetToMidBaseline;
                vertices[vertexIndex + 2] += offsetToMidBaseline;
                vertices[vertexIndex + 3] += offsetToMidBaseline;

                // Upload the mesh with the revised information
            }

            _text.UpdateVertexData();
            _isForceUpdatingMesh = false;
        }
    }
}
