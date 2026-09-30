/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-18
 */

using UnityEngine;

namespace Falcon.Modules.FPrefabPool.Runtime
{
    // Spawn sống lâu: chủ động gọi Release. Generation guard chặn release nhầm sau respawn.
    public readonly struct PooledHandle<T> where T : Component
    {
        private readonly FPrefabPool<T> _owner;
        private readonly PoolMember _member; // giữ thẳng để IsLive/Release khỏi tra dict + GetInstanceID
        private readonly uint _generation;
        public T Value { get; }

        internal PooledHandle(FPrefabPool<T> owner, T value, PoolMember member, uint generation)
        {
            _owner = owner;
            Value = value;
            _member = member;
            _generation = generation;
        }

        // Còn dùng được không: spawn ok, chưa destroy, đúng generation (gồm cả trường hợp pool Dispose).
        public bool HasValue => _owner != null && _owner.IsLive(_member, _generation);

        // Lấy instance an toàn: false + null nếu đã release/destroy/respawn. Ưu tiên dùng thay vì đọc Value trực tiếp.
        public bool TryGet(out T value)
        {
            if (HasValue) { value = Value; return true; }
            value = null;
            return false;
        }

        public void Release()
        {
            if (_owner == null) return;
            _owner.ReleaseFromHandle(Value, _member, _generation);
        }
    }
}
