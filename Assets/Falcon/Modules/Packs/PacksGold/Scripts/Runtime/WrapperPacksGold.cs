/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Packs.Core.Runtime;
using UnityEngine.AddressableAssets;

namespace Falcon.Modules.Packs.PacksGold.Runtime
{
    /// <summary>
    /// Wrapper chính quản lý các gói vật phẩm trong hệ thống PacksGold.
    /// Đảm nhiệm việc đăng ký lắng nghe sự kiện mở shop, và phát sự kiện tạo các pack UI tương ứng.
    /// </summary>
    public class WrapperPacksGold : ABaseWrapperPack<BasicElementPackConfig, NullPackUserData>
    {
        protected const string EVENT_CREATE_PACK_SHOP = "falcon.modules.shop.create_pack_shop";
        protected const string EVENT_HIDE_PACK_SHOP = "falcon.modules.shop.hide_pack_shop";
        public override string Key => "PacksGold";
        
        private static readonly AssetReference _assetPackGridGold = new("UIGrid_PacksGold");
        private static readonly AssetReference _assetPackGold = new("UIPack_Gold");
        
        protected override void AfterGetData()
        {
            base.AfterGetData();
            
            GameEvent<string>.Emit(EVENT_HIDE_PACK_SHOP, "grid_packs_gold");
            foreach (var element in DictConfigs)
            {
                GameEvent<string>.Emit(EVENT_HIDE_PACK_SHOP, element.Key);
            }

            if (Params != null && Params.TryGetValue("isGrid", out var value) && value == "1")
            {
                GameEvent<(AssetReference, string)>.Emit(EVENT_CREATE_PACK_SHOP, (_assetPackGridGold, "grid_packs_gold"));
            }
            else
            {
                foreach (var element in DictConfigs)
                {
                    GameEvent<(AssetReference, string)>.Emit(EVENT_CREATE_PACK_SHOP, (_assetPackGold, element.Key));
                }
            }
        }
    }
}