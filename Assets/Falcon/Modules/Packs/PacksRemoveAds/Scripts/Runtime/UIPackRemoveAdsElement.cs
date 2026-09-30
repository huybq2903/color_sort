/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using Falcon.Modules.Packs.Core.Runtime;

namespace Falcon.Modules.Packs.PacksRemoveAds.Runtime
{
    public class UIPackRemoveAdsElement : APackElement<WrapperPacksRemoveAds>
    {
        protected override void OnBuySuccess()
        {
            base.OnBuySuccess();
            _wrapper.BuyRemoveAds();
        }
    }
}