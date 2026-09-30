/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using Falcon.Helpers.FReflection;

namespace Falcon.Modules.Core.Network
{
    public interface ISessionListener : IFReflection
    {
        public void OnSessionReset();
        public void OnFirstSession();
        public void OnChannelDisconnected(FChannel channel);
    }
}

