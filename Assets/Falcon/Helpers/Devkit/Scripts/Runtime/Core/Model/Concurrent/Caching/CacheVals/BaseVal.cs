/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;

namespace Falcon.Helpers.Devkit
{
    public class BaseVal<T> : ICacheVal<T>
    {
        private T val;

        public BaseVal(T val)
        {
            this.val = val;
            LastAccessed = CreatedAt;
        }

        public DateTime CreatedAt { get; } = DateTime.Now;

        public DateTime LastAccessed { get; private set; }

        public int AccessCount { get; private set; }

        // Mỗi subclass sẽ định nghĩa cách kiểm tra hết hạn
        public bool IsExpired()
        {
            return false;
        }

        public bool TryGet(out T value)
        {
            LastAccessed = DateTime.Now;
            AccessCount++;
            value = val;
            return true;
        }

        public void Set(T value)
        {
            LastAccessed = DateTime.Now;
            AccessCount++;
            val = value;
        }
    }
}