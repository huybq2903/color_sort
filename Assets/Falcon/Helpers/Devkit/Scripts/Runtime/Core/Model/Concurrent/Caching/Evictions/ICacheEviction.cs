/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;
using System.Collections.Concurrent;

namespace Falcon.Helpers.Devkit
{
    public interface ICacheEviction
    {
        void Evict<TK, TV>(ConcurrentDictionary<TK, ICacheVal<TV>> dict, int size,
            Action<EvictionInfo<TK, TV>> evictionCallback);
    }

    public enum EvictionType
    {
        Fifo,
        Lfu,
        Lru
    }

    public static class EvictionTypeExtension
    {
        public static void Evict<TK, TV>(this EvictionType type, ConcurrentDictionary<TK, ICacheVal<TV>> dict, int size,
            Action<EvictionInfo<TK, TV>> evictionCallback)
        {
            switch (type)
            {
                case EvictionType.Lru:
                    CacheLru.Instance.Evict(dict, size, evictionCallback);
                    break;
                case EvictionType.Lfu:
                    CacheLfu.Instance.Evict(dict, size, evictionCallback);
                    break;
                case EvictionType.Fifo:
                    CacheFifo.Instance.Evict(dict, size, evictionCallback);
                    break;
                default:
                    CacheFifo.Instance.Evict(dict, size, evictionCallback);
                    break;
            }
        }
    }
}