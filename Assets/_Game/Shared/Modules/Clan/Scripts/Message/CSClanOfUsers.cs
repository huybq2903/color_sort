using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Clan
{
    [FAMessage("cs_clan_of_users")]
    public class CSClanOfUsers : CSMessageWaitLoginSuccess
    {
        public List<int> userCodes;
        public CSClanOfUsers(List<int> userCodes)
        {
            this.userCodes = userCodes;
        }
        public CSClanOfUsers(int userCode)
        {
            this.userCodes = new List<int>
            {
                userCode
            };
        }
    }
}
