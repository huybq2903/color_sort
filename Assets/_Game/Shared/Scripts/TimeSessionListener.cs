// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-08

using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using Falcon.Shared.Common.Time;
using UnityEngine;

public class TimeSessionListener : ISessionListener
{
    public void OnSessionReset()
    {
        SetSystemTime();
    }

    public void OnFirstSession()
    {
        WrapperTime.onFocus = SetSystemTime;
        SetSystemTime();
    }

    public void OnChannelDisconnected(FChannel channel)
    {

    }

    private static void SetSystemTime()
    {
        WrapperTime.OverrideTime(-1);
        if (Application.internetReachability == NetworkReachability.NotReachable || !AccountManager.Instance.IsLogin)
        {
            return;
        }

        new FTimePing().AddSCListener<FTimePong>((message, _, success) =>
        {
            if (success)
                WrapperTime.OverrideTime(message.timeServer);
        }).Send();
    }
}