using Falcon.Modules.Core.Network;
/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-09
 */
namespace Falcon.Modules.ABTestingByServerLogic.Scripts.Runtime
{
    [FAMessage("sc_set_ab_testing_value")]
    public class SCSetABTestingValue : SCMessage
    {
        public string ab_testing_campaign;
        public string ab_testing_value;
        public override void OnData()
        {
            ABTestingManager.Instance.Ab_testing_campaign = ab_testing_campaign;
            ABTestingManager.Instance.Ab_testing_value = ab_testing_value;
        }
    }
}