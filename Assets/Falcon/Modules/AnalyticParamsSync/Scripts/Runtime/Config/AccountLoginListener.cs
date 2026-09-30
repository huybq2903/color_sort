/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */

using System.Data;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.AccountData;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    public class AccountLoginListener
    {
        public static ConnectionState State { get; private set; } = ConnectionState.Connecting;

        public static async Task<bool> WaitLogin()
        {
            if (!await ServerSessionStateListener.WaitConnect()) return false;

            Timer timer = new();
            while (
                State == ConnectionState.Connecting
                && timer.TotalMillis() <= 15 * 1000
                && !AccountManager.Instance.IsLogin
            )
                await Task.Yield();
            State = AccountManager.Instance.IsLogin ? ConnectionState.Open : ConnectionState.Broken;

            return State == ConnectionState.Open;
        }
    }
}