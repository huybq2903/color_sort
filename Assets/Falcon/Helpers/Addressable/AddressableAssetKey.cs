/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-05
 */

using System;
using UnityEngine.AddressableAssets;

namespace Falcon.Helpers.Addressable
{
    /// <summary>
    /// Identity chung cho cache: hỗ trợ cả path (string) và AssetReference.
    /// </summary>
    internal readonly struct AddressableAssetKey : IEquatable<AddressableAssetKey>
    {
        private readonly string _key;
        private readonly int _hash;

        private AddressableAssetKey(string key)
        {
            _key = key;
            _hash = key != null ? key.GetHashCode() : 0;
        }

        public bool IsValid => !string.IsNullOrEmpty(_key);

        /// <summary>Chuỗi key đã chuẩn hoá (path đã Trim, hoặc RuntimeKey GUID) — nguồn sự thật cho cả cache lẫn load call.</summary>
        public string Value => _key;

        public static bool TryFromPath(string path, out AddressableAssetKey key)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                key = default;
                return false;
            }
            key = new AddressableAssetKey(path.Trim());
            return true;
        }

        public static bool TryFromReference(AssetReference reference, out AddressableAssetKey key)
        {
            if (reference == null || !reference.RuntimeKeyIsValid())
            {
                key = default;
                return false;
            }
            var k = reference.RuntimeKey;
            var keyStr = k?.ToString();
            if (string.IsNullOrEmpty(keyStr))
            {
                key = default;
                return false;
            }
            key = new AddressableAssetKey(keyStr);
            return true;
        }

        public bool Equals(AddressableAssetKey other) => string.Equals(_key, other._key, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AddressableAssetKey other && Equals(other);
        public override int GetHashCode() => _hash;
        public override string ToString() => _key ?? "(invalid)";

        public static bool operator ==(AddressableAssetKey left, AddressableAssetKey right) => left.Equals(right);
        public static bool operator !=(AddressableAssetKey left, AddressableAssetKey right) => !left.Equals(right);
    }
}
