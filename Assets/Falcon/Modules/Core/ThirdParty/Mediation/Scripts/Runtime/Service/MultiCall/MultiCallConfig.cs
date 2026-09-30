/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */

using Falcon.Modules.Core.RemoteConfigCms;

namespace Falcon.Modules.Core.ThirdParty.Mediation.Runtime
{
    public class MultiCallConfig : FConfigCms
    {
        public bool enableLogMultiCall = false;
        public int moMulProviderIndexMax = 0;
        public int moMulProviderIndexIronSource = 0;
        public int moMulProviderIndexAdmob = 0;
        public int minRamMultiCall = 0;
        public int periodDecreaseFloor = 5;
        public bool blockMulticallFromMobileData = false;
        public int numberAdsToLoadNext = 3;

        public override void OnData()
        {
        }
    }
}