/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System.Data;
using System.Threading.Tasks;
using Falcon.Modules.Core.Network;
using UnityEngine.Scripting;
using Timer = Falcon.Helpers.Devkit.Timer;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    public class ServerSessionStateListener : ISessionListener
    {
        public static ConnectionState State { get; private set; } = ConnectionState.Connecting;

        [Preserve]
        public ServerSessionStateListener()
        {
        }
        public static async Task<bool> WaitConnect()
        {
            Timer timer = new ();
            while (State == ConnectionState.Connecting)
            {
                await Task.Yield();
                // đợi 15s mà không ping được tới server thì kệ
                if (timer.TotalMillis() > 15 * 1000)
                {
                    State = ConnectionState.Broken;
                }
            }

            return State == ConnectionState.Open;
        }
        
        public void OnSessionReset()
        {
            State = ConnectionState.Open;
        }

        public void OnFirstSession()
        {
            State = ConnectionState.Open;
        }

        public void OnChannelDisconnected(FChannel channel)
        {
            State = ConnectionState.Broken;
        }
    }
}