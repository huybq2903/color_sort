/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-15


using System;
using Falcon.Modules.Core.RemoteConfig;

namespace Falcon.Modules.Core.AccountData
{
    [Serializable]
    public class RemoteConfig4Network : IFalconConfig
    {
        public string connection_uri;
    }
}