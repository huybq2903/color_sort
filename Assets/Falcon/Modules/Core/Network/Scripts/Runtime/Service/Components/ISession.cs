/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using System;
using System.Collections.Generic;

namespace Falcon.Modules.Core.Network
{
    public interface ISession
    {
        void Start();
        void OnChannelConnected();
        void OnChannelDisconnected();
        void OnMessage(FMessage message);
        void Send(CSMessage message);
        FChannel GetChannel();
    }
}

