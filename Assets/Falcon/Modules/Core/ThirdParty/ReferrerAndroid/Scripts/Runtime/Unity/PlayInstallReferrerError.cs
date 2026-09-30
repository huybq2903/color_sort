/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-11-17
 */

using System;

namespace Falcon.Modules.Core.ThirdParty.AndroidReferrer.Runtime
{
    public class PlayInstallReferrerError
    {
        public int ResponseCode { get; }
        public Exception Exception { get; }

        internal PlayInstallReferrerError(int responseCode, Exception exception)
        {
            ResponseCode = responseCode;
            Exception = exception;
        }
    }
}
