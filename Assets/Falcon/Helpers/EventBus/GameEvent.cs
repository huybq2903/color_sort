/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-05
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Falcon.Helpers.EventBus
{
    // @by-design: main-thread only, caller-enforced.
    /// <remarks>
    /// Không thread-safe. Mọi thao tác (Register/Unregister/Emit) phải chạy trên Unity main thread.
    /// Thứ tự gọi listener KHÔNG đảm bảo (HashSet order) — không được phụ thuộc thứ tự Register.
    /// </remarks>
    public static class GameEvent<T>
    {
        // Mỗi event-name một Channel: listener-set + snapshot bất biến gom chung một chỗ -> Emit chỉ 1 lookup.
        private sealed class Channel
        {
            public readonly HashSet<Action<T>> Listeners = new();
            public Action<T>[] Snapshot; // null = cần rebuild
        }

        // @by-design: string-key để decouple; chi phí hash mỗi Emit chấp nhận (event thưa).
        private static readonly Dictionary<string, Channel> kChannels = new();

        // Prefix log kèm type-param, tính 1 lần/closed-type (dùng cả ở build).
        // @by-design: vài chuỗi nhỏ/closed-type là trade-off cố ý.
        private static readonly string kLog = $"GameEvent<{typeof(T).Name}>";

#if UNITY_EDITOR
        // Hậu tố hiển thị theo T cho cây viewer (Editor); runtime vẫn key theo eventName thô.
        // @by-design: concat chỉ chạy trong #if UNITY_EDITOR (viewer), không vào build.
        private static readonly string kViewerSuffix = $" <{typeof(T).Name}>";

        // Editor-only: eventName -> (action -> Component) để Unregister tự tra listener đã truyền lúc Register.
        private static readonly Dictionary<string, Dictionary<Action<T>, Component>> kEditorListeners = new();

        static GameEvent() => EventBusReset.Register(Clear);

        // Domain Reload bị tắt trong Editor -> xóa các listener còn sót từ phiên Play trước.
        private static void Clear()
        {
            kChannels.Clear();
            kEditorListeners.Clear();
        }
#endif

        /// <summary>Thêm <paramref name="action"/> vào event <paramref name="eventName"/>; trùng reference bị bỏ qua (warning).</summary>
        /// <param name="eventName">Tên event. Nên dùng hằng chuỗi (constant string).</param>
        /// <param name="action">Action được gọi khi event được bắn.</param>
        /// <param name="listener">Chỉ cho Editor viewer (no-op khi build); KHÔNG tự Unregister. Null nếu không phải Component.</param>
        /// <returns>Handle gỡ đúng đăng ký này qua Dispose(); handle rỗng (no-op) nếu Register thất bại/trùng.</returns>
        public static EventSubscription<T> Register(string eventName, Action<T> action, Component listener = null)
        {
            if (EventBusGuard.NameInvalid(eventName, kLog)) return EventSubscription<T>.None;

            if (action == null)
            {
                Debug.LogError($"{kLog} > Register null action for event [{eventName}].");
                return EventSubscription<T>.None;
            }

            if (!kChannels.TryGetValue(eventName, out var channel))
                kChannels[eventName] = channel = new Channel();

            if (!channel.Listeners.Add(action))
            {
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning("This action has already registered for this event");
                return EventSubscription<T>.None; // trùng reference -> call này không tạo đăng ký mới
            }

            channel.Snapshot = null; // invalidate -> rebuild lazy ở Emit kế tiếp

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EventBusTypeRegistry.Events.Record(eventName, typeof(Action<T>), kLog);
#endif

#if UNITY_EDITOR
            if (listener == null)
                listener = action.Target as Component;
            if (listener != null)
            {
                if (!kEditorListeners.TryGetValue(eventName, out var map))
                    kEditorListeners[eventName] = map = new Dictionary<Action<T>, Component>();
                map[action] = listener;
                EventBusViewer.Events.OnRegister(eventName + kViewerSuffix, listener);
            }
#endif

            return new EventSubscription<T>(eventName, action);
        }

        /// <summary>Gỡ <paramref name="action"/> khỏi event <paramref name="eventName"/> (đúng reference đã Register).</summary>
        public static void Unregister(string eventName, Action<T> action)
        {
            if (EventBusGuard.NameInvalid(eventName, kLog)) return;

            if (action == null)
            {
                Debug.LogError($"{kLog} > Unregister null action for event [{eventName}].");
                return;
            }

            if (!kChannels.TryGetValue(eventName, out var channel) || !channel.Listeners.Remove(action))
            {
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning($"This event: {eventName} doesn't contain this action!");
                return;
            }

            if (channel.Listeners.Count == 0)
                kChannels.Remove(eventName);
            else
                channel.Snapshot = null; // giải phóng delegate đã gỡ ngay; rebuild lazy ở Emit kế tiếp

#if UNITY_EDITOR
            // listener được tra ngược từ map đã ghi lúc Register -> caller không cần truyền lại.
            if (kEditorListeners.TryGetValue(eventName, out var map) && map.Remove(action, out var listener))
            {
                if (map.Count == 0) kEditorListeners.Remove(eventName);
                EventBusViewer.Events.OnUnregister(eventName + kViewerSuffix, listener);
            }
#endif
        }

        // @by-design: overload tương thích cho 125+ call-site cũ; listener bị bỏ qua (viewer tự tra) — chấp nhận thừa, khỏi migrate.
        public static void Unregister(string eventName, Action<T> action, Component listener) => Unregister(eventName, action);

        /// <summary>Gỡ toàn bộ listener của event <paramref name="eventName"/>.</summary>
        public static void UnregisterAll(string eventName)
        {
            if (EventBusGuard.NameInvalid(eventName, kLog)) return;

            if (!kChannels.Remove(eventName))
            {
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning($"This event: {eventName} has no register yet!");
                return;
            }

#if UNITY_EDITOR
            kEditorListeners.Remove(eventName);
            EventBusViewer.Events.OnUnregisterAll(eventName + kViewerSuffix);
#endif
        }

        // @by-design: payload by-value; caller chọn type nhỏ.
        /// <summary>Bắn event tới mọi listener; no-op nếu chưa ai đăng ký.</summary>
        public static void Emit(string eventName, T data = default)
        {
            // @by-design: null name -> ArgumentNullException (fail-loud chủ đích); không guard vì perf.
            if (!kChannels.TryGetValue(eventName, out var channel))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                EventBusTypeRegistry.Events.WarnIfMismatch(eventName, typeof(Action<T>), kLog);
#endif
                return;
            }

            var snapshot = channel.Snapshot;
            // @by-design: cố ý new mảng mỗi lần rebuild để vòng Emit (kể cả re-entrant) luôn duyệt
            // snapshot bất biến; alloc chỉ xảy ra khi subscriber đổi — chỉ tối ưu nếu profiler báo GC.
            if (snapshot == null)
            {
                snapshot = new Action<T>[channel.Listeners.Count];
                channel.Listeners.CopyTo(snapshot);
                channel.Snapshot = snapshot;
            }

            // @by-design: per-listener try cho isolation; chỉ gỡ EH khi profiler báo.
            foreach (var listener in snapshot)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // @by-design: loud diagnostic editor/dev-only — bắt footgun quên Unregister; KHÔNG throttle (spam = tín hiệu sửa ngay).
                if (listener.Target is Object target && !target)
                {
                    Debug.LogError($"{kLog} > '{eventName}': listener target đã bị Destroy mà chưa Unregister. Bỏ qua invoke.");
                    continue;
                }
#endif
                try
                {
                    listener.Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"{kLog} > Exception invoking '{eventName}': {e}");
                }
            }
        }
    }

    // @by-design: GameEvent / GameEvent<T> cố ý nhân đôi thân hàm — static generic không share
    // static state, và gộp về GameEvent<Unit> sẽ mất tối ưu no-arg zero-overhead.
    // @by-design: main-thread only, caller-enforced.
    /// <summary>
    /// Event bus cho các event không tham số.
    /// </summary>
    /// <remarks>
    /// Không thread-safe. Mọi thao tác (Register/Unregister/Emit) phải chạy trên Unity main thread.
    /// Thứ tự gọi listener KHÔNG đảm bảo (HashSet order) — không được phụ thuộc thứ tự Register.
    /// </remarks>
    public static class GameEvent
    {
        // Mỗi event-name một Channel: listener-set + snapshot bất biến gom chung một chỗ -> Emit chỉ 1 lookup.
        private sealed class Channel
        {
            public readonly HashSet<Action> Listeners = new();
            public Action[] Snapshot; // null = cần rebuild
        }

        // @by-design: string-key để decouple; chi phí hash mỗi Emit chấp nhận (event thưa).
        private static readonly Dictionary<string, Channel> kChannels = new();

#if UNITY_EDITOR
        // Hậu tố hiển thị cho cây viewer (Editor) — event không tham số.
        // @by-design: concat chỉ chạy trong #if UNITY_EDITOR (viewer), không vào build.
        private const string kViewerSuffix = " <void>";

        // Editor-only: eventName -> (action -> Component) để Unregister tự tra listener đã truyền lúc Register.
        private static readonly Dictionary<string, Dictionary<Action, Component>> kEditorListeners = new();

        static GameEvent() => EventBusReset.Register(Clear);

        // Domain Reload bị tắt trong Editor -> xóa các listener còn sót từ phiên Play trước.
        private static void Clear()
        {
            kChannels.Clear();
            kEditorListeners.Clear();
        }
#endif

        /// <summary>Thêm <paramref name="action"/> vào event không tham số <paramref name="eventName"/>; trùng reference bị bỏ qua (warning).</summary>
        /// <param name="eventName">Tên event. Nên dùng hằng chuỗi (constant string).</param>
        /// <param name="action">Action được gọi khi event được bắn.</param>
        /// <param name="listener">Chỉ cho Editor viewer (no-op khi build); KHÔNG tự Unregister. Null nếu không phải Component.</param>
        /// <returns>Handle gỡ đúng đăng ký này qua Dispose(); handle rỗng (no-op) nếu Register thất bại/trùng.</returns>
        public static EventSubscription Register(string eventName, Action action, Component listener = null)
        {
            if (EventBusGuard.NameInvalid(eventName, nameof(GameEvent))) return EventSubscription.None;

            if (action == null)
            {
                Debug.LogError($"{nameof(GameEvent)} > Register null action for event [{eventName}].");
                return EventSubscription.None;
            }

            if (!kChannels.TryGetValue(eventName, out var channel))
                kChannels[eventName] = channel = new Channel();

            if (!channel.Listeners.Add(action))
            {
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning("This action has already registered for this event");
                return EventSubscription.None; // trùng reference -> call này không tạo đăng ký mới
            }

            channel.Snapshot = null; // invalidate -> rebuild lazy ở Emit kế tiếp

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EventBusTypeRegistry.Events.Record(eventName, typeof(Action), nameof(GameEvent));
#endif

#if UNITY_EDITOR
            if (listener == null)
                listener = action.Target as Component;
            if (listener != null)
            {
                if (!kEditorListeners.TryGetValue(eventName, out var map))
                    kEditorListeners[eventName] = map = new Dictionary<Action, Component>();
                map[action] = listener;
                EventBusViewer.Events.OnRegister(eventName + kViewerSuffix, listener);
            }
#endif

            return new EventSubscription(eventName, action);
        }

        /// <summary>Gỡ <paramref name="action"/> khỏi event <paramref name="eventName"/> (đúng reference đã Register).</summary>
        public static void Unregister(string eventName, Action action)
        {
            if (EventBusGuard.NameInvalid(eventName, nameof(GameEvent))) return;

            if (action == null)
            {
                Debug.LogError($"{nameof(GameEvent)} > Unregister null action for event [{eventName}].");
                return;
            }

            if (!kChannels.TryGetValue(eventName, out var channel) || !channel.Listeners.Remove(action))
            {
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning($"This event: {eventName} doesn't contain this action!");
                return;
            }

            if (channel.Listeners.Count == 0)
                kChannels.Remove(eventName);
            else
                channel.Snapshot = null; // giải phóng delegate đã gỡ ngay; rebuild lazy ở Emit kế tiếp

#if UNITY_EDITOR
            // listener được tra ngược từ map đã ghi lúc Register -> caller không cần truyền lại.
            if (kEditorListeners.TryGetValue(eventName, out var map) && map.Remove(action, out var listener))
            {
                if (map.Count == 0) kEditorListeners.Remove(eventName);
                EventBusViewer.Events.OnUnregister(eventName + kViewerSuffix, listener);
            }
#endif
        }

        // @by-design: overload tương thích cho 125+ call-site cũ; listener bị bỏ qua (viewer tự tra) — chấp nhận thừa, khỏi migrate.
        public static void Unregister(string eventName, Action action, Component listener) => Unregister(eventName, action);

        /// <summary>Gỡ toàn bộ listener của event <paramref name="eventName"/>.</summary>
        public static void UnregisterAll(string eventName)
        {
            if (EventBusGuard.NameInvalid(eventName, nameof(GameEvent))) return;

            if (!kChannels.Remove(eventName))
            {
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning($"This event: {eventName} has no register yet!");
                return;
            }

#if UNITY_EDITOR
            kEditorListeners.Remove(eventName);
            EventBusViewer.Events.OnUnregisterAll(eventName + kViewerSuffix);
#endif
        }

        /// <summary>Bắn event tới mọi listener; no-op nếu chưa ai đăng ký.</summary>
        public static void Emit(string eventName)
        {
            // @by-design: null name -> ArgumentNullException (fail-loud chủ đích); không guard vì perf.
            if (!kChannels.TryGetValue(eventName, out var channel))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                EventBusTypeRegistry.Events.WarnIfMismatch(eventName, typeof(Action), nameof(GameEvent));
#endif
                return;
            }

            var snapshot = channel.Snapshot;
            // @by-design: cố ý new mảng mỗi lần rebuild để vòng Emit (kể cả re-entrant) luôn duyệt
            // snapshot bất biến; alloc chỉ xảy ra khi subscriber đổi — chỉ tối ưu nếu profiler báo GC.
            if (snapshot == null)
            {
                snapshot = new Action[channel.Listeners.Count];
                channel.Listeners.CopyTo(snapshot);
                channel.Snapshot = snapshot;
            }

            // @by-design: per-listener try cho isolation; chỉ gỡ EH khi profiler báo.
            foreach (var listener in snapshot)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // @by-design: loud diagnostic editor/dev-only — bắt footgun quên Unregister; KHÔNG throttle (spam = tín hiệu sửa ngay).
                if (listener.Target is Object target && !target)
                {
                    Debug.LogError($"{nameof(GameEvent)} > '{eventName}': listener target đã bị Destroy mà chưa Unregister. Bỏ qua invoke.");
                    continue;
                }
#endif
                try
                {
                    listener.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"{nameof(GameEvent)} > Exception invoking '{eventName}': {e}");
                }
            }
        }
    }

    /// <summary>
    /// Handle gỡ đăng ký của <see cref="GameEvent{T}.Register"/>: Dispose() gỡ đúng (eventName, action) đã đăng ký
    /// mà caller không cần giữ delegate reference. Idempotent (Dispose nhiều lần an toàn);
    /// <see cref="None"/> là handle rỗng no-op trả về khi Register thất bại/trùng.
    /// </summary>
    // @by-design: class để Dispose idempotent, chống gỡ nhầm; alloc chỉ ở Register (cold-path).
    public sealed class EventSubscription<T> : IDisposable
    {
        internal static readonly EventSubscription<T> None = new(null, null);

        private string _eventName;
        private Action<T> _action;

        internal EventSubscription(string eventName, Action<T> action)
        {
            _eventName = eventName;
            _action = action;
        }

        public void Dispose()
        {
            if (_action == null) return; // None / đã Dispose -> no-op
            GameEvent<T>.Unregister(_eventName, _action);
            _eventName = null;
            _action = null;
        }
    }

    /// <summary>Handle gỡ đăng ký của <see cref="GameEvent.Register"/> (event không tham số). Xem <see cref="EventSubscription{T}"/>.</summary>
    // @by-design: class để Dispose idempotent, chống gỡ nhầm; alloc chỉ ở Register (cold-path).
    public sealed class EventSubscription : IDisposable
    {
        internal static readonly EventSubscription None = new(null, null);

        private string _eventName;
        private Action _action;

        internal EventSubscription(string eventName, Action action)
        {
            _eventName = eventName;
            _action = action;
        }

        public void Dispose()
        {
            if (_action == null) return; // None / đã Dispose -> no-op
            GameEvent.Unregister(_eventName, _action);
            _eventName = null;
            _action = null;
        }
    }
}
