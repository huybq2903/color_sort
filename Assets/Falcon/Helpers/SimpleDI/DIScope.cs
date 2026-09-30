/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-26
 */

using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Falcon.Helpers.SimpleDI
{
    public class DIScope : IDisposable
    {
        // Thread-Safe dictionary
        private readonly ConcurrentDictionary<Type, object> _instances = new();

        public DIScope Bind<T>(T instance) where T : class
        {
            var key = typeof(T);

            if (_instances.ContainsKey(key))
            {
                Debug.LogWarning($"[DI] Type {typeof(T).Name} đã tồn tại -> Rebind.");
            }

            // Nếu chưa có thì nó thêm mới, nếu có rồi nó sẽ ghi đè
            _instances[key] = instance;
            return this;
        }

        public T Resolve<T>() where T : class
        {
            var key = typeof(T);

            if (_instances.TryGetValue(key, out var instance))
            {
                return (T)instance;
            }

            Debug.LogWarning($"[DI] Type {typeof(T).Name} chưa được bind trong scope này.");
            return null;
        }

        public DIScope Unbind<T>() where T : class
        {
            var key = typeof(T);
            if (!_instances.TryRemove(key, out var instance)) return this;

            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
            return this;
        }

        // Gỡ binding CHỈ KHI nó đúng là instance này — OnDestroy chạy trễ (vd 2 instance overlap
        // khi additive-load scene) sẽ không xóa nhầm binding của instance mới.
        public DIScope UnbindIf<T>(T instance) where T : class
        {
            if (_instances.TryGetValue(typeof(T), out var current) && ReferenceEquals(current, instance))
                Unbind<T>();
            return this;
        }

        // Giải phóng bộ nhớ khi Scope không còn được dùng đến nữa
        public void Dispose()
        {
            foreach (var kvp in _instances)
            {
                // Nếu instance có implement IDisposable, gọi Dispose để nó tự dọn dẹp (hủy event...)
                if (kvp.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            
            _instances.Clear();
        }
    }
}