/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-03
 */

using Falcon.Modules.Core.Network;

namespace Game.Shared.Profile
{
    [FAMessage("cs_get_profile")]
    public class CSGetProfile : CSMessage
    {
        public int code;
        
        public CSGetProfile(int code)
        {
            this.code = code;
        }
    }
}