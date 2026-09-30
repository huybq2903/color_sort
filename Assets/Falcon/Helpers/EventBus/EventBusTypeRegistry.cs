/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-06-04
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Falcon.Helpers.EventBus
{
    // Dev-only: bắt footgun cùng 1 name dùng với delegate-shape khác nhau giữa Register và Emit/Request.
    // Mỗi closed-generic bus giữ static-store RIÊNG -> lệch T = miss câm; gom 1 chỗ để phát hiện.
    // Tách Events/Requests (giống EventBusViewer) để name trùng giữa hai hệ không báo nhầm.
    internal sealed class EventBusTypeRegistry
    {
        internal static readonly EventBusTypeRegistry Events = new();
        internal static readonly EventBusTypeRegistry Requests = new();

#if UNITY_EDITOR
        // Domain Reload tắt -> xóa map còn sót từ phiên Play trước.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            Events._nameToType.Clear();
            Requests._nameToType.Clear();
        }
#endif

        private readonly Dictionary<string, Type> _nameToType = new();

        // Register: ghi shape lần đầu; cùng name khác shape -> warning (dual-claim, có thể bridge cố ý). Miss câm THẬT do WarnIfMismatch (Emit/Request) bắt và vẫn log đỏ.
        internal void Record(string name, Type shape, string logPrefix)
        {
            if (_nameToType.TryGetValue(name, out var known))
            {
                // if (known != shape)
                //     Debug.LogWarning($"{logPrefix} > '{name}' đăng ký 2 shape: [{Describe(known)}] và [{Describe(shape)}]. OK nếu bridge cố ý; nếu không → nguy cơ miss câm.");
                return;
            }
            _nameToType[name] = shape;
        }

        // Nhánh no-op của Emit/Request: name có thật nhưng khác shape -> đây là miss câm thật sự.
        internal void WarnIfMismatch(string name, Type shape, string logPrefix)
        {
            if (_nameToType.TryGetValue(name, out var known) && known != shape)
                Debug.LogError($"{logPrefix} > '{name}' dùng [{Describe(shape)}] nhưng đã đăng ký dưới [{Describe(known)}] — KHÔNG khớp.");
        }

        // Tên đọc được cho message (chỉ chạy ở nhánh mismatch hiếm).
        private static string Describe(Type shape)
        {
            if (shape == typeof(Action)) return "void";
            var args = shape.GetGenericArguments();
            return args.Length == 1 ? args[0].Name : $"{args[0].Name}->{args[1].Name}";
        }
    }
}
#endif
