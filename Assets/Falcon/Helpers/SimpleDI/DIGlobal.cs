/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-26
 */

using UnityEngine;

namespace Falcon.Helpers.SimpleDI
{
    public static class DIGlobal
    {
        private static readonly DIScope _globalScope = new();

        public static DIScope Bind<T>(T instance) where T : class
            => _globalScope.Bind(instance);

        public static T Resolve<T>() where T : class
            => _globalScope.Resolve<T>();

        public static DIScope Unbind<T>() where T : class
            => _globalScope.Unbind<T>();

        public static DIScope UnbindIf<T>(T instance) where T : class
            => _globalScope.UnbindIf(instance);

        public static void ClearAll()
        {
            _globalScope.Dispose();
        }

#if UNITY_EDITOR

        // Trên Unity (khi tắt Domain Reload để vào game nhanh) sẽ KHÔNG reset các biến static.
        // Hàm này tự động dọn dẹp DIGlobal mỗi lần ấn nút Play trên Editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStateOnPlay()
        {
            ClearAll();
        }

#endif
    }
}