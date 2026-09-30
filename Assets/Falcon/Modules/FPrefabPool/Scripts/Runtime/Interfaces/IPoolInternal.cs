/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-20
 */

using System;

namespace Falcon.Modules.FPrefabPool.Runtime
{
    // View type-erased của FPrefabPool<T> để PoolRegistry quản lý pool qua 1 list chung (Dispose, prune).
    internal interface IPoolInternal : IDisposable
    {
        bool IsDisposed { get; }

        // Prefab nguồn đã unload (Unity-null) → pool stale, cần Dispose để dọn holder + instance orphan.
        bool IsStale { get; }

        // GameObject instanceID của prefab — CHUNG cho mọi T của cùng prefab. Registry dùng để phát hiện
        // cùng 1 prefab bị pool dưới nhiều component type T (tạo pool trùng, gấp đôi instance).
        int PrefabGameObjectId { get; }
    }
}
