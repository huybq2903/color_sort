/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-06-04
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Falcon.Helpers.EventBus
{
#if UNITY_EDITOR
    // Cây viewer dùng chung, chỉ có trong Editor. Mỗi bus (event vs request) một instance để
    // tên event và tên request trùng chuỗi không đụng nhau trong cùng một dictionary.
    internal sealed class EventBusViewer
    {
        internal static readonly EventBusViewer Events = new("GameEventViewer");
        internal static readonly EventBusViewer Requests = new("GameRequestViewer");

        // Static trong Editor sống sót qua Play khi tắt Domain Reload -> reset cả hai cây viewer.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            Events.Reset();
            Requests.Reset();
        }

        private readonly string _rootName;
        private GameObject _root;
        private readonly Dictionary<string, EventViewer> _viewers = new();

        private EventBusViewer(string rootName) => _rootName = rootName;

        private void Reset()
        {
            _root = null;
            _viewers.Clear();
        }

        internal void OnRegister(string eventName, Component listener)
        {
            if (_root == null)
            {
                _root = new GameObject(_rootName);
                Object.DontDestroyOnLoad(_root);
                _viewers.Clear(); // root chết -> mọi viewer con chết theo; bỏ entry fake-null tồn đọng
            }

            // viewer GameObject có thể bị xoá tay trong Hierarchy lúc Play -> fake-null; coi như miss và tạo lại.
            if (_viewers.TryGetValue(eventName, out var viewer) && viewer)
            {
                viewer.AddListener(listener);
                return;
            }

            // @by-design: viewer editor-only; tạo/huỷ GameObject là cách hiển thị.
            var go = new GameObject(eventName);
            go.transform.SetParent(_root.transform);
            viewer = go.AddComponent<EventViewer>();
            viewer.AddListener(listener);
            _viewers[eventName] = viewer; // indexer: ghi đè entry chết (nếu có) thay vì Add ném ArgumentException
        }

        internal void OnUnregister(string eventName, Component listener)
        {
            if (!_viewers.TryGetValue(eventName, out var viewer)) return;
            if (!viewer) // entry chết (fake-null) -> dọn
            {
                _viewers.Remove(eventName);
                return;
            }

            viewer.RemoveListener(listener);
            if (viewer.ListenerCount > 0) return;

            viewer.DestroySelf();
            _viewers.Remove(eventName);
        }

        internal void OnUnregisterAll(string eventName)
        {
            if (!_viewers.Remove(eventName, out var viewer)) return;
            if (viewer) viewer.DestroySelf(); // viewer có thể đã bị xoá tay -> fake-null
        }
    }

    /// <summary>
    /// Chỉ dùng trong Editor. Domain Reload bị tắt nên state static của EventBus sống sót qua các
    /// phiên Play. Mỗi class bus đăng ký Clear() của nó vào đây; ResetAll() chạy mỗi lần Play để
    /// xóa listener/responder còn sót từ phiên trước.
    /// </summary>
    internal static class EventBusReset
    {
        private static readonly List<Action> kClearActions = new();

        internal static void Register(Action clear) => kClearActions.Add(clear);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            for (int i = 0; i < kClearActions.Count; i++) kClearActions[i].Invoke();
        }
    }
#endif
}
