/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Falcon.Helpers.Devkit
{
    public class MyConcurrentCache<TK, TV> : IDictionary<TK, TV>
    {
        private readonly CacheBuilder<TK, TV> _builder;

        private readonly ConcurrentDictionary<TK, ICacheVal<TV>> _cache = new();

        private readonly Action<EvictionInfo<TK, TV>> _evictionCallback;

        public MyConcurrentCache(CacheBuilder<TK, TV> builder, Action<EvictionInfo<TK, TV>> evictionCallback)
        {
            _builder = builder;
            _evictionCallback = evictionCallback;
            Keys = new MyCacheKeys<TK, TV>(_cache, _evictionCallback);
            Values = new MyCacheValues<TK, TV>(_cache, _evictionCallback);
        }

        public TV this[TK key]
        {
            get => TryGetValue(key, out var value) ? value : default;
            set => Add(key, value);
        }

        public ICollection<TK> Keys { get; }
        public ICollection<TV> Values { get; }

        public int Count => _cache.Count;

        public bool IsReadOnly => false;

        public void Add(TK key, TV value)
        {
            _cache.Compute(key, cacheVal =>
            {
                if (cacheVal != null)
                    _evictionCallback(!cacheVal.TryGet(out var val)
                        ? new EvictionInfo<TK, TV>(EvictionReason.Expired, key, val)
                        : new EvictionInfo<TK, TV>(EvictionReason.Replaced, key, val, value));

                return _builder.Wrap(value);
            });
            Cleanup();
        }

        void ICollection<KeyValuePair<TK, TV>>.Add(KeyValuePair<TK, TV> item)
        {
            Add(item.Key, item.Value);
        }

        public bool ContainsKey(TK key)
        {
            return TryGetValue(key, out _);
        }

        public bool Remove(TK key)
        {
            return _cache.TryRemove(key, out _);
        }

        public bool TryGetValue(TK key, out TV value)
        {
            var result = false;
            var val = default(TV);
            _cache.Compute(key, cacheVal =>
            {
                if (cacheVal == null) return null;

                if (cacheVal.TryGet(out val))
                {
                    result = true;
                    return cacheVal;
                }

                _evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Expired, key, val));
                return null;
            });
            value = val;
            return result;
        }

        public void Clear()
        {
            Keys.Clear();
        }

        bool ICollection<KeyValuePair<TK, TV>>.Contains(KeyValuePair<TK, TV> item)
        {
            return TryGetValue(item.Key, out var value) && Equals(value, item.Value);
        }

        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex)
        {
            foreach (var kv in this) array[arrayIndex++] = kv;
        }

        public bool Remove(KeyValuePair<TK, TV> item)
        {
            var result = false;
            _cache.Compute(item.Key, cacheVal =>
            {
                if (cacheVal == null) return null;

                if (!cacheVal.TryGet(out var val))
                {
                    _evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Expired, item.Key, val));
                    return null;
                }

                if (!Equals(val, item.Value)) return cacheVal;
                result = true;
                _evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Removed, item.Key, val));
                return null;
            });
            return result;
        }

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator()
        {
            foreach (var (key, cacheVal) in _cache)
                if (cacheVal.TryGet(out var val))
                    yield return new KeyValuePair<TK, TV>(key, val);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event Action<EvictionInfo<TK, TV>> OnEviction
        {
            add => _builder.OnEviction += value;
            remove => _builder.OnEviction -= value;
        }

        public TV GetOrLoad(TK key, Func<TK, TV> loader)
        {
            TV result = default;
            var wroteSmt = false;
            _cache.Compute(key, cacheVal =>
            {
                if (cacheVal == null)
                {
                    wroteSmt = true;
                    result = loader(key);
                    return _builder.Wrap(result);
                }

                if (cacheVal.TryGet(out result)) return cacheVal;

                var before = result;
                result = loader(key);
                cacheVal = _builder.Wrap(result);
                _evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Expired, key, before, result));
                return cacheVal;
            });

            if (wroteSmt) Cleanup();

            return result;
        }

        public void Cleanup()
        {
            foreach (var key in _cache.Keys)
                _cache.Compute(key, cacheVal =>
                {
                    if (cacheVal.TryGet(out var result)) return cacheVal;
                    _evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Expired, key, result));
                    return null;
                });

            if (!_builder.Limit.HasValue) return;
            var tuple = _builder.Limit.Value;
            if (_cache.Count < tuple.size) return;
            tuple.evictionType.Evict(_cache, tuple.size, _evictionCallback);
        }

        public SealedBox<TV> Compute(TK key, Func<SealedBox<TV>, SealedBox<TV>> func)
        {
            var result = SealedBox<TV>.Instance;
            _cache.Compute(key, cacheVal =>
            {
                if (cacheVal == null)
                {
                    result = func(result);
                    return result.TryGetValue(out var tempResult) ? _builder.Wrap(tempResult) : null;
                }

                if (cacheVal.TryGet(out var val))
                {
                    result = new SealedBox<TV>(val);
                    return cacheVal;
                }

                _evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Expired, key, val));
                result = func(result);
                return result.TryGetValue(out val) ? _builder.Wrap(val) : null;
            });
            return result;
        }
    }
}