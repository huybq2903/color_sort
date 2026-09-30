/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Falcon.Helpers.Devkit
{
    public class CacheLru : UtilSingleton<CacheLru>, ICacheEviction
    {
        public void Evict<TK, TV>(ConcurrentDictionary<TK, ICacheVal<TV>> dict, int size,
            Action<EvictionInfo<TK, TV>> evictionCallback)
        {
            foreach (var k in dict
                         .Select(entry => (entry.Key, entry.Value.LastAccessed))
                         .OrderBy(e => e.LastAccessed)
                         .Select(e => e.Key))
            {
                if (dict.Count <= size) break;
                if (!dict.TryRemove(k, out var cacheVal)) continue;
                evictionCallback.Invoke(cacheVal.TryGet(out var val)
                    ? new EvictionInfo<TK, TV>(EvictionReason.Size, k, val)
                    : new EvictionInfo<TK, TV>(EvictionReason.Expired, k, val));
            }
        }
    }
}