/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-06-04
 */

using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Falcon.Helpers.EventBus
{
#if UNITY_EDITOR
    internal sealed class EventViewer : MonoBehaviour
    {
        // refcount cho O(1) + đếm; _listeners chỉ để hiện unique trong Inspector.
        private readonly Dictionary<Component, int> _refCount = new();
        [SerializeField] private List<Component> _listeners = new();

        internal int ListenerCount { get; private set; } // tổng action-registration

        internal void AddListener(Component component)
        {
            ListenerCount++;
            if (_refCount.TryGetValue(component, out var n)) { _refCount[component] = n + 1; return; }
            _refCount[component] = 1;
            _listeners.Add(component); // chỉ thêm vào list hiển thị lần đầu
        }

        internal void RemoveListener(Component component)
        {
            if (!_refCount.TryGetValue(component, out var n)) return;
            ListenerCount--;
            if (n > 1) { _refCount[component] = n - 1; return; }
            _refCount.Remove(component);
            _listeners.Remove(component); // @by-design: O(n) editor-only, chỉ khi gỡ component cuối.
        }

        internal void DestroySelf() => Object.Destroy(gameObject);
    }
#endif
}
