/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-26
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Falcon.Helpers.EventBus
{
    // @by-design: main-thread only, caller-enforced.
    /// <remarks>
    /// Không thread-safe. Mọi thao tác (Register/Unregister/Request) phải chạy trên Unity main thread.
    /// </remarks>
    public static class GameRequest<T>
    {
        // @by-design: string-key để decouple; chi phí hash mỗi Request chấp nhận (request thưa).
        private static readonly Dictionary<string, Func<T>> kRequestDict = new();

        // Prefix log kèm type-param, tính 1 lần/closed-type (dùng cả ở build).
        // @by-design: vài chuỗi nhỏ/closed-type là trade-off cố ý.
        private static readonly string kLog = $"GameRequest<{typeof(T).Name}>";

#if UNITY_EDITOR
        // Hậu tố hiển thị theo T cho cây viewer (Editor); runtime vẫn key theo requestName thô.
        // @by-design: concat chỉ chạy trong #if UNITY_EDITOR (viewer), không vào build.
        private static readonly string kViewerSuffix = $" <{typeof(T).Name}>";

        static GameRequest() => EventBusReset.Register(Clear);

        // Domain Reload bị tắt trong Editor -> xóa các responder còn sót từ phiên Play trước.
        private static void Clear() => kRequestDict.Clear();
#endif

        /// <summary>
        /// Đăng ký một request để cung cấp dữ liệu khi được hỏi.
        /// </summary>
        /// <param name="requestName">Tên request. Nên dùng hằng chuỗi (constant string).</param>
        /// <param name="responder">Hàm trả về dữ liệu cho request.</param>
        /// <param name="listener">Chỉ cho Editor viewer (no-op khi build); KHÔNG tự Unregister. Null nếu không phải Component.</param>
        public static void Register(string requestName, Func<T> responder, Component listener = null)
        {
            if (EventBusGuard.NameInvalid(requestName, kLog)) return;

            if (responder == null)
            {
                Debug.LogError($"{kLog} > Register null responder for request [{requestName}].");
                return;
            }

            if (!kRequestDict.TryAdd(requestName, responder))
            {
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning($"Request [{requestName}] already has a responder registered.");
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EventBusTypeRegistry.Requests.Record(requestName, typeof(Func<T>), kLog);
#endif

#if UNITY_EDITOR
            if (listener == null)
                listener = responder.Target as Component;
            if (listener != null)
                EventBusViewer.Requests.OnRegister(requestName + kViewerSuffix, listener);
#endif
        }

        // @by-design: single-responder + requestName là module-scoped const nên va chạm owner cực hiếm; gỡ theo tên là idempotent cleanup cố ý.
        public static void Unregister(string requestName)
        {
            if (EventBusGuard.NameInvalid(requestName, kLog)) return;
            if (!kRequestDict.Remove(requestName)) return;

#if UNITY_EDITOR
            EventBusViewer.Requests.OnUnregisterAll(requestName + kViewerSuffix);
#endif
        }

        // Gom invoke + catch chung cho Request/TryRequest; false nếu responder ném.
        private static bool TryInvoke(string requestName, Func<T> responder, out T result)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // @by-design: loud diagnostic editor/dev-only — bắt footgun quên Unregister; KHÔNG throttle (spam = tín hiệu sửa ngay).
            if (responder.Target is Object target && !target)
            {
                Debug.LogError($"{kLog} > [{requestName}]: responder target đã bị Destroy mà chưa Unregister.");
                result = default;
                return false;
            }
#endif
            try
            {
                result = responder.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{kLog} > Error invoking responder for [{requestName}]: {ex}");
                result = default;
                return false;
            }
        }

        /// <summary>Hỏi data từ responder; trả <c>default</c> kèm warning nếu chưa đăng ký. Cần phân biệt "không có data" thì dùng <see cref="TryRequest"/>.</summary>
        public static T Request(string requestName)
        {
            if (EventBusGuard.NameInvalid(requestName, kLog)) return default;

            if (!kRequestDict.TryGetValue(requestName, out var responder))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                EventBusTypeRegistry.Requests.WarnIfMismatch(requestName, typeof(Func<T>), kLog);
#endif
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning($"No responder registered for request [{requestName}].");
                return default;
            }

            _ = TryInvoke(requestName, responder, out var result);
            return result;
        }

        /// <summary>
        /// Như <see cref="Request"/> nhưng phân biệt được "không có data" với "data = default".
        /// </summary>
        /// <returns>true nếu có responder và chạy xong; false nếu thiếu responder hoặc responder ném.</returns>
        public static bool TryRequest(string requestName, out T result)
        {
            // "try" không bao giờ throw: tên null/rỗng → false thay vì để TryGetValue ném.
            if (string.IsNullOrEmpty(requestName))
            {
                result = default;
                return false;
            }

            // Khác Request: KHÔNG cảnh báo khi thiếu responder — giá trị bool trả về chính là tín hiệu cho caller tự xử.
            if (!kRequestDict.TryGetValue(requestName, out var responder))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                EventBusTypeRegistry.Requests.WarnIfMismatch(requestName, typeof(Func<T>), kLog);
#endif
                result = default;
                return false;
            }

            return TryInvoke(requestName, responder, out result);
        }
    }

    // @by-design: GameRequest<T> / GameRequest<TP,T> cố ý nhân đôi thân hàm — static generic không
    // share static state; tách riêng theo arity để giữ no-param zero-overhead.
    // @by-design: main-thread only, caller-enforced.
    /// <remarks>
    /// Không thread-safe. Mọi thao tác (Register/Unregister/Request) phải chạy trên Unity main thread.
    /// </remarks>
    public static class GameRequest<TP, T>
    {
        // @by-design: string-key để decouple; chi phí hash mỗi Request chấp nhận (request thưa).
        private static readonly Dictionary<string, Func<TP, T>> kRequestDict = new();

        // Prefix log kèm type-param, tính 1 lần/closed-type (dùng cả ở build).
        // @by-design: vài chuỗi nhỏ/closed-type là trade-off cố ý.
        private static readonly string kLog = $"GameRequest<{typeof(TP).Name},{typeof(T).Name}>";

#if UNITY_EDITOR
        // Hậu tố hiển thị theo (TP,T) cho cây viewer (Editor); runtime vẫn key theo requestName thô.
        // @by-design: concat chỉ chạy trong #if UNITY_EDITOR (viewer), không vào build.
        private static readonly string kViewerSuffix = $" <{typeof(TP).Name},{typeof(T).Name}>";

        static GameRequest() => EventBusReset.Register(Clear);

        // Domain Reload bị tắt trong Editor -> xóa các responder còn sót từ phiên Play trước.
        private static void Clear() => kRequestDict.Clear();
#endif

        /// <summary>Đăng ký responder có tham số cho request <paramref name="requestName"/>.</summary>
        /// <param name="listener">Chỉ cho Editor viewer (no-op khi build); KHÔNG tự Unregister. Null nếu không phải Component.</param>
        public static void Register(string requestName, Func<TP, T> responder, Component listener = null)
        {
            if (EventBusGuard.NameInvalid(requestName, kLog)) return;

            if (responder == null)
            {
                Debug.LogError($"{kLog} > Register null responder for request [{requestName}].");
                return;
            }

            if (!kRequestDict.TryAdd(requestName, responder))
            {
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning($"Request [{requestName}] already has a responder registered.");
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EventBusTypeRegistry.Requests.Record(requestName, typeof(Func<TP, T>), kLog);
#endif

#if UNITY_EDITOR
            if (listener == null)
                listener = responder.Target as Component;
            if (listener != null)
                EventBusViewer.Requests.OnRegister(requestName + kViewerSuffix, listener);
#endif
        }

        // @by-design: single-responder + requestName là module-scoped const nên va chạm owner cực hiếm; gỡ theo tên là idempotent cleanup cố ý.
        public static void Unregister(string requestName)
        {
            if (EventBusGuard.NameInvalid(requestName, kLog)) return;
            if (!kRequestDict.Remove(requestName)) return;

#if UNITY_EDITOR
            EventBusViewer.Requests.OnUnregisterAll(requestName + kViewerSuffix);
#endif
        }

        // Gom invoke + catch chung cho Request/TryRequest; false nếu responder ném.
        private static bool TryInvoke(string requestName, Func<TP, T> responder, TP param, out T result)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // @by-design: loud diagnostic editor/dev-only — bắt footgun quên Unregister; KHÔNG throttle (spam = tín hiệu sửa ngay).
            if (responder.Target is Object target && !target)
            {
                Debug.LogError($"{kLog} > [{requestName}]: responder target đã bị Destroy mà chưa Unregister.");
                result = default;
                return false;
            }
#endif
            try
            {
                result = responder.Invoke(param);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{kLog} > Error invoking responder for [{requestName}]: {ex}");
                result = default;
                return false;
            }
        }

        // @by-design: param by-value; caller chọn type nhỏ.
        public static T Request(string requestName, TP param)
        {
            if (EventBusGuard.NameInvalid(requestName, kLog)) return default;

            if (!kRequestDict.TryGetValue(requestName, out var responder))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                EventBusTypeRegistry.Requests.WarnIfMismatch(requestName, typeof(Func<TP, T>), kLog);
#endif
                // @by-design: vi phạm contract có thể phục hồi; mức warning là có chủ đích
                Debug.LogWarning($"No responder registered for request [{requestName}].");
                return default;
            }

            _ = TryInvoke(requestName, responder, param, out var result);
            return result;
        }

        /// <summary>
        /// Như <see cref="Request"/> nhưng phân biệt được "không có data" với "data = default".
        /// </summary>
        /// <returns>true nếu có responder và chạy xong; false nếu thiếu responder hoặc responder ném.</returns>
        public static bool TryRequest(string requestName, TP param, out T result)
        {
            // "try" không bao giờ throw: tên null/rỗng → false thay vì để TryGetValue ném.
            if (string.IsNullOrEmpty(requestName))
            {
                result = default;
                return false;
            }

            // Khác Request: KHÔNG cảnh báo khi thiếu responder — giá trị bool trả về chính là tín hiệu cho caller tự xử.
            if (!kRequestDict.TryGetValue(requestName, out var responder))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                EventBusTypeRegistry.Requests.WarnIfMismatch(requestName, typeof(Func<TP, T>), kLog);
#endif
                result = default;
                return false;
            }

            return TryInvoke(requestName, responder, param, out result);
        }
    }
}