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
using System.Linq;

namespace Falcon.Helpers.Devkit
{
    public class MyCacheValues<TK, TV> : ICollection<TV>
    {
        private readonly ConcurrentDictionary<TK, ICacheVal<TV>> cache;
        private readonly Action<EvictionInfo<TK, TV>> evictionCallback;

        public MyCacheValues(ConcurrentDictionary<TK, ICacheVal<TV>> cache,
            Action<EvictionInfo<TK, TV>> evictionCallback)
        {
            this.cache = cache;
            this.evictionCallback = evictionCallback;
        }

        public IEnumerator<TV> GetEnumerator()
        {
            foreach (var key in cache.Keys)
                if (TryAccess(key, out var val))
                    yield return val;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TV item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            foreach (var key in cache.Keys)
                cache.Compute(key, cacheVal =>
                {
                    if (cacheVal == null) return null;

                    if (!cacheVal.TryGet(out var val))
                    {
                        evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Expired, key, val));
                        return null;
                    }

                    evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Removed, key, val));
                    return null;
                });
        }

        public bool Contains(TV item)
        {
            return this.Any(v => Equals(v, item));
        }

        public void CopyTo(TV[] array, int arrayIndex)
        {
            foreach (var kv in this) array[arrayIndex++] = kv;
        }

        public bool Remove(TV item)
        {
            return cache.Keys.Any(key => TryRemove(key, item));
        }

        public int Count => cache.Count;
        public bool IsReadOnly => true;

        private bool TryAccess(TK key, out TV value)
        {
            var result = false;
            var val = default(TV);
            cache.Compute(key, cacheVal =>
            {
                if (cacheVal == null) return null;

                if (cacheVal.TryGet(out val))
                {
                    result = true;
                    return cacheVal;
                }

                evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Expired, key, val));
                return null;
            });
            value = val;
            return result;
        }

        private bool TryRemove(TK key, TV value)
        {
            var result = false;
            cache.Compute(key, cacheVal =>
            {
                if (cacheVal == null) return null;

                if (!cacheVal.TryGet(out var val))
                {
                    evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Expired, key, val));
                    return null;
                }

                if (!Equals(val, value)) return cacheVal;
                result = true;
                evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Removed, key, val));
                return null;
            });
            return result;
        }
    }
}