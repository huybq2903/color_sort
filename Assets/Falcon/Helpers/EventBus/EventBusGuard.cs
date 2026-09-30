/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-06-04
 */

using System.Runtime.CompilerServices;
using UnityEngine;

namespace Falcon.Helpers.EventBus
{
    // Guard cold-path dùng chung cho GameEvent/GameRequest (mọi arity). Emit cố ý KHÔNG gọi (giữ zero-overhead).
    internal static class EventBusGuard
    {
        // true = tên null/empty (đã log đỏ). [CallerMemberName] tự điền tên method gọi -> message vẫn định vị được.
        internal static bool NameInvalid(string name, string logPrefix, [CallerMemberName] string op = null)
        {
            if (!string.IsNullOrEmpty(name)) return false;
            Debug.LogError($"{logPrefix} > {op} with null/empty name.");
            return true;
        }
    }
}
