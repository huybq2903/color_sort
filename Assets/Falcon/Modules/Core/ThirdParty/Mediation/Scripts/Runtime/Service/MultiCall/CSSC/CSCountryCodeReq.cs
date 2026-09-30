/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-04
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FAMessage("cs_country_code_req")]
    public class CSCountryCodeReq : CSMessage
    {
        public int code;

        public CSCountryCodeReq()
        {
        }

        public CSCountryCodeReq(int code)
        {
            this.code = code;
        }
    }
}