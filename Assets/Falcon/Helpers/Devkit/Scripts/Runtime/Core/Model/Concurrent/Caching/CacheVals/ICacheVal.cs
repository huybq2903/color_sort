/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-28
 */
using System;

namespace Falcon.Helpers.Devkit
{
    public interface ICacheVal<T>
    {
        DateTime CreatedAt { get; }
        DateTime LastAccessed { get; }
        int AccessCount { get; }
        bool IsExpired();
        bool TryGet(out T value);
        void Set(T value);
    }
}