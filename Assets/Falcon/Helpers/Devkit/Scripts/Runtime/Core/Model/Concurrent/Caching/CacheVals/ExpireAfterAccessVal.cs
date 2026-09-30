/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;

namespace Falcon.Helpers.Devkit
{
    public class ExpireAfterAccessVal<TValue> : ICacheVal<TValue>
    {
        private readonly TimeSpan _ttl;
        private readonly ICacheVal<TValue> val;
        public ExpireAfterAccessVal(TimeSpan ttl, ICacheVal<TValue> val)
        {
            _ttl = ttl;
            this.val = val;
        }
        public DateTime CreatedAt => val.CreatedAt;
        public DateTime LastAccessed => val.LastAccessed;
        public int AccessCount => val.AccessCount;

        public bool IsExpired()
        {
            return DateTime.UtcNow - LastAccessed > _ttl || val.IsExpired();
        }

        public bool TryGet(out TValue value)
        {
            bool result = val.TryGet(out value);
            return DateTime.UtcNow - LastAccessed > _ttl && result;
        }

        public void Set(TValue value)
        {
            val.Set(value);
        }
    }
}