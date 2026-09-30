/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-04
 */

using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    [FAMessage("sc_country_code_rsp")]
    public class SCCountryCodeRsp : SCMessage
    {
        public string countryCode;

        public override void OnData()
        {
        }
    }
}