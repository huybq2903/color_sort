/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;

namespace Falcon.Helpers.Devkit
{
    public class WeakRefVal<T> : ICacheVal<T> where T : class
    {
        private readonly WeakReference<T> valRef;

        public WeakRefVal(T value)
        {
            valRef = new WeakReference<T>(value);
            LastAccessed = CreatedAt;
        }

        public DateTime CreatedAt { get; } = DateTime.Now;

        public DateTime LastAccessed { get; private set; }
        public int AccessCount { get; private set; }

        public bool IsExpired()
        {
            return valRef.TryGetTarget(out _);
        }

        public bool TryGet(out T value)
        {
            LastAccessed = DateTime.Now;
            AccessCount++;
            return valRef.TryGetTarget(out value);
        }

        public void Set(T value)
        {
            LastAccessed = DateTime.Now;
            AccessCount++;
            valRef.SetTarget(value);
        }
    }
}