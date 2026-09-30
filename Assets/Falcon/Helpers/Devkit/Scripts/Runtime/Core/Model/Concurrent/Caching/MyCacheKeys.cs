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
    public class MyCacheKeys<TK, TV> : ICollection<TK>
    {
        private readonly ConcurrentDictionary<TK, ICacheVal<TV>> cache;
        private readonly Action<EvictionInfo<TK, TV>> evictionCallback;

        public MyCacheKeys(ConcurrentDictionary<TK, ICacheVal<TV>> cache, Action<EvictionInfo<TK, TV>> evictionCallback)
        {
            this.cache = cache;
            this.evictionCallback = evictionCallback;
        }

        public IEnumerator<TK> GetEnumerator()
        {
            return cache.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TK item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            foreach (var key in cache.Keys)
                Remove(key);
        }

        public bool Contains(TK item)
        {
            return !Equals(item, null) && cache.ContainsKey(item);
        }

        public void CopyTo(TK[] array, int arrayIndex)
        {
            foreach (var kv in this) array[arrayIndex++] = kv;
        }

        public bool Remove(TK item)
        {
            if(Equals(item, null)) return false;
            bool result = false;
            cache.Compute(item, cacheVal =>
            {
                if (cacheVal == null) return null;

                if (!cacheVal.TryGet(out var val))
                {
                    evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Expired, item, val));
                    return null;
                }

                evictionCallback(new EvictionInfo<TK, TV>(EvictionReason.Removed, item, val));
                result = true;
                return null;
            });
            return result;
        }

        public int Count => cache.Count;
        public bool IsReadOnly => true;
    }
}
