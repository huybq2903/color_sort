/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-02
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using UnityEngine;

namespace Falcon.Modules.Account4EventBus.Runtime
{
    public static class Account4EventBus
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            AccountManager.Instance.OnLoginEvent += OnLogin;
        }

        private static void OnLogin(bool success)
        {
            GameEvent<bool>.Emit("falcon.modules.account.login", success);
        }
    }
}