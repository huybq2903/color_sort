/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-18
 */

using System;
using UnityEngine;

namespace Falcon.Modules.FPrefabPool.Runtime
{
    // Spawn ngắn hạn: dùng trong `using` => Dispose tự Release. Generation guard chặn release nhầm sau respawn.
    // Compose PooledHandle để dùng chung logic safety (HasValue/TryGet/Release) — 1 nguồn sự thật duy nhất.
    public readonly struct PooledLease<T> : IDisposable where T : Component
    {
        private readonly PooledHandle<T> _handle;

        internal PooledLease(FPrefabPool<T> owner, T value, PoolMember member, uint generation)
            => _handle = new PooledHandle<T>(owner, value, member, generation);

        public T Value => _handle.Value;
        public bool HasValue => _handle.HasValue;
        public bool TryGet(out T value) => _handle.TryGet(out value);
        public void Release() => _handle.Release();

        // Idempotent: auto-dispose (cuối `using`) là no-op khi handle đã hết hiệu lực (đã Release/respawn/destroy)
        // → không spam Warning ở pattern hợp lệ `using` + Release() thủ công. Release() tường minh vẫn fail-loud.
        public void Dispose() { if (_handle.HasValue) _handle.Release(); }
    }
}
