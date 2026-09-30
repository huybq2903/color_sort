/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-03
 */

using System.Collections.Generic;
using Falcon.Modules.Core.Network;

namespace Game.Shared.Profile
{
    [FAMessage("sc_get_profile")]
    public class SCGetProfile : SCMessage
    {
        public int code;
        public int avatarId;
        public int frameId;
        public string playerName;
        public string avatarUrl;
        public string createDate;
        public string countryCode;
        public int level;
        public Dictionary<string, int> stats = new();
        public override void OnData()
        {
            
        }
    }
}