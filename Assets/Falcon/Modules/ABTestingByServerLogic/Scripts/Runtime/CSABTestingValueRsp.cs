using Falcon.Modules.Core.Network;
/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-09
 */
namespace Falcon.Modules.ABTestingByServerLogic.Scripts.Runtime
{
    [FAMessage("cs_ab_testing_value_rsp")]
    public class CSABTestingValueRsp : CSMessage
    {
        public string ab_testing_campaign;
        public string ab_testing_value;

        public CSABTestingValueRsp()
        {
            this.ab_testing_campaign = ABTestingManager.Instance.Ab_testing_campaign;
            this.ab_testing_value = ABTestingManager.Instance.Ab_testing_value;
        }
    }
}