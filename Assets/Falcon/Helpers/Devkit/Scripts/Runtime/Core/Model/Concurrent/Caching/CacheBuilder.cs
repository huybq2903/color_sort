/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;

namespace Falcon.Helpers.Devkit
{
    public class CacheBuilder<TK, TV>
    {
        public Func<TV, ICacheVal<TV>> ValFactory { get; set; } = t => new BaseVal<TV>(t);
        public Func<ICacheVal<TV>, ICacheVal<TV>> ValWrappings { get; set; } = t => t;
        public TimeSpan? ExpireAfterAccess { get; set; }
        public TimeSpan? ExpireAfterWrite { get; set; }
        
        public event Action<EvictionInfo<TK,TV>> OnEviction; 

        public (int size, EvictionType evictionType)? Limit { get; set; }

        public ICacheVal<TV> Wrap(TV val)
        {
            return ValWrappings(ValFactory(val));
        }
    }

    public static class CacheBuilderExtensions
    {
        public static CacheBuilder<TK, TV> WeakVal<TK, TV>(this CacheBuilder<TK, TV> builder) where TV : class
        {
            builder.ValFactory = t => new WeakRefVal<TV>(t);
            return builder;
        }
    }
}