using Falcon.Modules.Core.Network;
/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-09
 */
namespace Falcon.Modules.ABTestingByServerLogic.Scripts.Runtime
{
    [FAMessage("sc_get_ab_testing_value")]
    public class SCGetABTestingValue : SCMessage
    {
        public override void OnData()
        {
            new CSABTestingValueRsp().Send();
        }
    }
}