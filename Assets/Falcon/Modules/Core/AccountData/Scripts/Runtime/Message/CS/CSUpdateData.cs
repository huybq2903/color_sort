/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.AccountData 
{
    [FAMessage("cs_update_data")]
    public class CSUpdateData : CSMessage
    {
        public ClientData clientData;

        public CSUpdateData(ClientData clientData) { this.clientData = clientData; }

    }
}