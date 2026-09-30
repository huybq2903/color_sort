using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor {

    [CustomEditor(typeof(SkeletonDataAsset)), CanEditMultipleObjects]
    public class SkeletonDataAssetCustomInspector : SkeletonDataAssetInspector {

        static readonly int TimelineHash = "CustomTimeline".GetHashCode();

        FieldInfo _previewField;
        PropertyInfo _activeTrackProp;
        MethodInfo _refreshMethod;
        MethodInfo _staticPreviewMethod;
        object _previewObj;

        // State: Add Event
        int    _animIndex;
        string _newEventName = "";
        float  _eventTime;

        // State: Delete Event
        int _deleteAnimIndex;

        // State: Multi-skin preview
        readonly HashSet<string> _selectedSkins = new HashSet<string>();
        bool _multiSkinFoldout = true;

        void EnsureReflection () {
            if (_previewField != null) return;
            var inspectorType = typeof(SkeletonDataAssetInspector);
            _previewField = inspectorType.GetField("preview", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_previewField == null) return;

            _previewObj = _previewField.GetValue(this);
            if (_previewObj == null) return;

            var previewType = _previewObj.GetType();
            _activeTrackProp     = previewType.GetProperty("ActiveTrack",       BindingFlags.Public | BindingFlags.Instance);
            _refreshMethod       = previewType.GetMethod("RefreshOnNextUpdate",  BindingFlags.Public | BindingFlags.Instance);
            _staticPreviewMethod = previewType.GetMethod("GetStaticPreview",     BindingFlags.Public | BindingFlags.Instance);
        }

        TrackEntry GetActiveTrack () {
            EnsureReflection();
            if (_previewField == null) return null;
            _previewObj = _previewField.GetValue(this);
            return _activeTrackProp?.GetValue(_previewObj) as TrackEntry;
        }

        void RefreshPreview () {
            if (_previewObj != null)
                _refreshMethod?.Invoke(_previewObj, null);
        }

        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        public override void OnInspectorGUI () {
            base.OnInspectorGUI();
            DrawMultiSkinSection();
            DrawAddEventSection();
        }

        void DrawMultiSkinSection () {
            var skeletonAsset = (SkeletonDataAsset)target;
            var skeletonData  = skeletonAsset.GetSkeletonData(false);
            if (skeletonData == null || skeletonData.Skins.Count <= 1) return;

            EditorGUILayout.Space();
            _multiSkinFoldout = EditorGUILayout.Foldout(_multiSkinFoldout, "Multi-Skin Preview", true, EditorStyles.foldoutHeader);
            if (!_multiSkinFoldout) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                bool changed = false;
                foreach (var skin in skeletonData.Skins) {
                    if (skin.Name == "default") continue;
                    bool isSelected = _selectedSkins.Contains(skin.Name);
                    bool newVal     = EditorGUILayout.ToggleLeft(skin.Name, isSelected);
                    if (newVal != isSelected) {
                        if (newVal) _selectedSkins.Add(skin.Name);
                        else        _selectedSkins.Remove(skin.Name);
                        changed = true;
                    }
                }

                if (changed) ApplyCombinedSkin(skeletonData);

                EditorGUILayout.Space(4);
                if (GUILayout.Button("Reset về default skin", EditorStyles.miniButton)) {
                    _selectedSkins.Clear();
                    ApplyCombinedSkin(skeletonData);
                }
            }
        }

        void ApplyCombinedSkin (SkeletonData skeletonData) {
            EnsureReflection();
            _previewObj = _previewField?.GetValue(this);
            if (_previewObj == null) return;

            var saField = _previewObj.GetType().GetField("skeletonAnimation", BindingFlags.NonPublic | BindingFlags.Instance);
            var sa      = saField?.GetValue(_previewObj) as SkeletonAnimation;
            if (sa?.Skeleton == null) return;

            if (_selectedSkins.Count == 0) {
                sa.Skeleton.Skin = skeletonData.DefaultSkin;
            } else {
                var combined = new Skin("__multi_preview__");
                // Luôn include default skin làm nền
                if (skeletonData.DefaultSkin != null)
                    combined.AddSkin(skeletonData.DefaultSkin);
                foreach (var skin in skeletonData.Skins) {
                    if (_selectedSkins.Contains(skin.Name))
                        combined.AddSkin(skin);
                }
                sa.Skeleton.Skin = combined;
            }

            sa.Skeleton.SetSlotsToSetupPose();
            RefreshPreview();
            Repaint();
        }

        void DrawAddEventSection () {
            var skeletonAsset = (SkeletonDataAsset)target;
            var skeletonData  = skeletonAsset.GetSkeletonData(false);
            if (skeletonData == null) return;

            EditorGUILayout.Space();

            // Kiểm tra JSON hay binary
            var jsonAsset = skeletonAsset.skeletonJSON;
            string jsonPath = jsonAsset != null ? AssetDatabase.GetAssetPath(jsonAsset) : null;
            bool isJson = jsonPath != null && jsonPath.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase);

            if (!isJson) {
                EditorGUILayout.LabelField("Event Editor", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Tính năng này chỉ hỗ trợ skeleton định dạng JSON.", MessageType.Warning);
                return;
            }

            var animNames     = skeletonData.Animations.Select(a => a.Name).ToArray();
            var existingNames = skeletonData.Events.Select(e => e.Name).ToArray();
            if (animNames.Length == 0) return;

            // ── Add Event ───────────────────────────────────────
            EditorGUILayout.LabelField("Add Event", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {

                _animIndex = Mathf.Clamp(_animIndex, 0, animNames.Length - 1);
                _animIndex = EditorGUILayout.Popup("Animation", _animIndex, animNames);

                using (new EditorGUILayout.HorizontalScope()) {
                    _eventTime = EditorGUILayout.FloatField("Time (s)", _eventTime);
                    var track  = GetActiveTrack();
                    using (new EditorGUI.DisabledGroupScope(track == null)) {
                        if (GUILayout.Button("← Preview", EditorStyles.miniButton, GUILayout.Width(72))) {
                            _eventTime = Mathf.Round((track.TrackTime % track.Animation.Duration) * 1000f) / 1000f;
                        }
                    }
                }

                _newEventName = EditorGUILayout.TextField("Tên event", _newEventName);

                EditorGUILayout.Space(4);
                using (new EditorGUI.DisabledGroupScope(string.IsNullOrWhiteSpace(_newEventName))) {
                    if (GUILayout.Button("+ Add Event", GUILayout.Height(26)))
                        AddEventToJson(jsonPath, animNames[_animIndex], _eventTime, _newEventName);
                }
            }

            // ── Delete Event ─────────────────────────────────────
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Delete Event", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                _deleteAnimIndex = Mathf.Clamp(_deleteAnimIndex, 0, animNames.Length - 1);
                _deleteAnimIndex = EditorGUILayout.Popup("Animation", _deleteAnimIndex, animNames);

                var selectedAnim = skeletonData.FindAnimation(animNames[_deleteAnimIndex]);
                var eventInstances = new System.Collections.Generic.List<(float time, string name)>();

                if (selectedAnim != null) {
                    foreach (var timeline in selectedAnim.Timelines) {
                        if (timeline is EventTimeline et) {
                            for (int i = 0; i < et.Events.Length; i++)
                                eventInstances.Add((et.Frames[i], et.Events[i].Data.Name));
                        }
                    }
                }

                if (eventInstances.Count == 0) {
                    EditorGUILayout.LabelField("Animation này không có event nào.", EditorStyles.miniLabel);
                } else {
                    EditorGUILayout.Space(2);
                    var oldColor = GUI.backgroundColor;
                    foreach (var (time, name) in eventInstances) {
                        using (new EditorGUILayout.HorizontalScope()) {
                            EditorGUILayout.LabelField($"{time:F3}s", GUILayout.Width(55));
                            EditorGUILayout.LabelField(name);
                            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                            if (GUILayout.Button("Xóa", EditorStyles.miniButton, GUILayout.Width(40))) {
                                DeleteEventInstanceFromJson(jsonPath, animNames[_deleteAnimIndex], time, name);
                            }
                            GUI.backgroundColor = oldColor;
                        }
                    }
                }
            }
        }

        void AddEventToJson (string jsonPath, string animName, float time, string eventName) {
            string raw   = File.ReadAllText(jsonPath);
            JObject root = JObject.Parse(raw);

            // Thêm EventData ở root nếu chưa có
            var eventsObj = (root["events"] as JObject) ?? new JObject();
            if (eventsObj[eventName] == null)
                eventsObj[eventName] = new JObject();
            root["events"] = eventsObj;

            // Thêm event instance vào animation
            var animations = root["animations"] as JObject;
            if (animations == null) { Debug.LogWarning("[AddEvent] Không tìm thấy animations trong JSON."); return; }

            if (!(animations[animName] is JObject anim)) {
                Debug.LogWarning($"[AddEvent] Không tìm thấy animation '{animName}' trong JSON.");
                return;
            }

            var animEvents = (anim["events"] as JArray) ?? new JArray();

            float rounded = Mathf.Round(time * 1000f) / 1000f;
            animEvents.Add(new JObject { ["time"] = rounded, ["name"] = eventName });

            // Sắp xếp theo time
            anim["events"] = new JArray(animEvents.OrderBy(e => e["time"]?.Value<float>() ?? 0f));

            File.WriteAllText(jsonPath, root.ToString(Newtonsoft.Json.Formatting.None));
            AssetDatabase.ImportAsset(jsonPath);
            AssetDatabase.Refresh();

            Debug.Log($"[AddEvent] '{eventName}' → '{animName}' tại {rounded:F3}s");
        }

        void DeleteEventInstanceFromJson (string jsonPath, string animName, float time, string eventName) {
            string raw   = File.ReadAllText(jsonPath);
            JObject root = JObject.Parse(raw);

            if (root["animations"] is JObject animations &&
                animations[animName] is JObject anim &&
                anim["events"] is JArray animEvents) {

                float rounded = Mathf.Round(time * 1000f) / 1000f;
                var remaining = new JArray(animEvents.Where(e =>
                    !(e["name"]?.Value<string>() == eventName &&
                      Mathf.Abs((e["time"]?.Value<float>() ?? 0f) - rounded) < 0.0001f)));

                if (remaining.Count > 0)
                    anim["events"] = remaining;
                else
                    anim.Remove("events");
            }

            File.WriteAllText(jsonPath, root.ToString(Newtonsoft.Json.Formatting.None));
            AssetDatabase.ImportAsset(jsonPath);
            AssetDatabase.Refresh();

            Debug.Log($"[DeleteEvent] Đã xóa '{eventName}' tại {time:F3}s khỏi animation '{animName}'.");
        }

        // -------------------------------------------------------
        // Preview
        // -------------------------------------------------------

        public override void OnInteractivePreviewGUI (Rect r, GUIStyle background) {
            base.OnInteractivePreviewGUI(r, background);
            DrawCustomTimeline(r);
        }

        void DrawCustomTimeline (Rect r) {
            TrackEntry track = GetActiveTrack();
            if (track?.Animation == null) return;

            float duration = track.Animation.Duration;
            if (duration <= 0f) return;

            float currentTime = track.TrackTime % duration;

            Rect bar = new Rect(r.x + 4, r.y, r.width - 8, 32);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel) {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white }
            };
            GUI.Label(new Rect(bar.x, bar.y + 8, bar.width, 16),
                      $"{currentTime:F2}s  /  {duration:F2}s", labelStyle);

            var e  = UnityEngine.Event.current;
            int id = GUIUtility.GetControlID(TimelineHash, FocusType.Passive, bar);

            switch (e.GetTypeForControl(id)) {
                case EventType.MouseDown:
                    if (e.button == 0 && bar.Contains(e.mousePosition)) {
                        GUIUtility.hotControl = id;
                        ApplyScrub(e.mousePosition, bar, track, duration);
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id) {
                        ApplyScrub(e.mousePosition, bar, track, duration);
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id) {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }

            if (bar.Contains(UnityEngine.Event.current.mousePosition))
                EditorGUIUtility.AddCursorRect(bar, MouseCursor.SlideArrow);
        }

        void ApplyScrub (Vector2 mousePos, Rect bar, TrackEntry track, float duration) {
            float t = Mathf.Clamp01((mousePos.x - bar.x) / bar.width);
            track.TrackTime = t * duration;
            track.TimeScale = 0f;
            RefreshPreview();
            Repaint();
        }

        // -------------------------------------------------------
        // Capture
        // -------------------------------------------------------

        public override void OnPreviewSettings () {
            base.OnPreviewSettings();
            if (GUILayout.Button("Capture", EditorStyles.miniButton, GUILayout.Width(60)))
                CaptureCurrentFrame();
        }

        void CaptureCurrentFrame () {
            EnsureReflection();
            _previewObj = _previewField?.GetValue(this);
            if (_previewObj == null) { Debug.LogWarning("[Capture] Preview chưa sẵn sàng."); return; }

            var previewType = _previewObj.GetType();
            var goField = previewType.GetField("previewGameObject",    BindingFlags.NonPublic | BindingFlags.Instance);
            var ruField = previewType.GetField("previewRenderUtility", BindingFlags.NonPublic | BindingFlags.Instance);
            var saField = previewType.GetField("skeletonAnimation",    BindingFlags.NonPublic | BindingFlags.Instance);

            var previewGO = goField?.GetValue(_previewObj) as GameObject;
            var ru        = ruField?.GetValue(_previewObj) as PreviewRenderUtility;
            var sa        = saField?.GetValue(_previewObj) as SkeletonAnimation;

            if (previewGO == null || ru == null) { Debug.LogWarning("[Capture] Hãy play animation trước."); return; }

            Camera cam = ru.camera;
            if (cam == null) return;

            var meshRenderer = previewGO.GetComponent<Renderer>();
            if (meshRenderer == null) return;

            meshRenderer.enabled = true;
            sa?.LateUpdate();
            Bounds bounds = meshRenderer.bounds;

            if (bounds.size.magnitude < 0.001f) {
                meshRenderer.enabled = false;
                Debug.LogWarning("[Capture] Không lấy được kích thước skeleton.");
                return;
            }

            var skeletonAsset = (SkeletonDataAsset)target;
            float scale   = skeletonAsset.scale > 0f ? skeletonAsset.scale : 0.01f;
            int texWidth  = Mathf.Max(1, Mathf.RoundToInt(bounds.size.x / scale));
            int texHeight = Mathf.Max(1, Mathf.RoundToInt(bounds.size.y / scale));

            var rt = new RenderTexture(texWidth, texHeight, 24, RenderTextureFormat.ARGB32);

            var prevTarget     = cam.targetTexture;
            var prevBgColor    = cam.backgroundColor;
            var prevClearFlags = cam.clearFlags;
            var prevOrthoSize  = cam.orthographicSize;
            var prevPosition   = cam.transform.position;

            cam.targetTexture      = rt;
            cam.backgroundColor    = Color.clear;
            cam.clearFlags         = CameraClearFlags.SolidColor;
            cam.orthographicSize   = bounds.size.y / 2f;
            cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, prevPosition.z);

            cam.Render();
            meshRenderer.enabled = false;

            RenderTexture.active = rt;
            var tex = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            cam.targetTexture      = prevTarget;
            cam.backgroundColor    = prevBgColor;
            cam.clearFlags         = prevClearFlags;
            cam.orthographicSize   = prevOrthoSize;
            cam.transform.position = prevPosition;

            DestroyImmediate(rt);

            string path = EditorUtility.SaveFilePanel("Lưu ảnh capture", "", $"{target.name}_capture.png", "png");
            if (string.IsNullOrEmpty(path)) { DestroyImmediate(tex); return; }

            File.WriteAllBytes(path, tex.EncodeToPNG());
            DestroyImmediate(tex);
            Debug.Log($"[Capture] Đã lưu {texWidth}x{texHeight}px: {path}");
            AssetDatabase.Refresh();
        }
    }
}
