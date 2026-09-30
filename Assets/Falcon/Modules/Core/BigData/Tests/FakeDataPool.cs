using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData.Tests
{
    /// <summary>
    /// IDataPool in-memory cho test chuyển-phiên: "phiên 2" = dựng service/cache MỚI nhưng đưa
    /// CÙNG một pool — pool đóng vai cái đĩa sống sót qua kill.
    /// <br/>⚠ [NoAutoCreate] BẮT BUỘC: FReflection quét cả test asmdef khi editor Play game thật,
    /// thiếu attribute này là fake chui vào container DI của game thay FDataPool thật (mìn đã ghi
    /// sổ của repo). KHÔNG dùng FDataPool thật ở đây vì nó ghi vào path cứng Sdk/DataNew.txt —
    /// test sẽ giẫm lên data thật của editor.
    /// <br/>Ngữ nghĩa mô phỏng theo FDataPool: giá trị null = xoá key.
    /// </summary>
    [NoAutoCreate]
    public class FakeDataPool : IDataPool
    {
        private readonly Dictionary<string, object> _store = new();
        private readonly object _lock = new();

        public T GetOrDefault<T>(string key, T defaultValue)
        {
            lock (_lock) return _store.TryGetValue(key, out var v) && v is T t ? t : defaultValue;
        }

        public T GetOrSet<T>(string key, T valueIfNotExist)
        {
            lock (_lock)
            {
                if (_store.TryGetValue(key, out var v) && v is T t) return t;
                _store[key] = valueIfNotExist;
                return valueIfNotExist;
            }
        }

        public bool HasKey(string key)
        {
            lock (_lock) return _store.ContainsKey(key);
        }

        public T Compute<T>(string key, Func<T, T> function) where T : class
        {
            lock (_lock)
            {
                var current = _store.TryGetValue(key, out var v) && v is T t ? t : null;
                var next = function(current);
                if (next == null) _store.Remove(key);
                else _store[key] = next;
                return next;
            }
        }

        public T? Compute<T>(string key, Func<T?, T?> function) where T : struct
        {
            lock (_lock)
            {
                T? current = _store.TryGetValue(key, out var v) && v is T t ? t : null;
                var next = function(current);
                if (next == null) _store.Remove(key);
                else _store[key] = next.Value;
                return next;
            }
        }

        public void Save<T>(string key, T value)
        {
            lock (_lock) _store[key] = value;
        }

        public void Delete(string key)
        {
            lock (_lock) _store.Remove(key);
        }

        public void Clear()
        {
            lock (_lock) _store.Clear();
        }

        /// <summary>Số lần code production đòi đẩy xuống đĩa — assert được tính kill-safe thật.</summary>
        public int SyncCount { get; private set; }

        public bool TrySync()
        {
            lock (_lock) SyncCount++;
            return true;
        }
    }
}
