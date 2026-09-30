/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-20
 */

using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Packs.Core.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Falcon.Modules.Packs.PacksRemoveAds.Runtime
{
    /// <summary>
    /// Wrapper chính quản lý các gói vật phẩm trong hệ thống PacksRemoveAds.
    /// Đảm nhiệm việc đăng ký lắng nghe sự kiện mở shop, và phát sự kiện tạo các pack UI tương ứng.
    /// </summary>
    public class WrapperPacksRemoveAds : ABaseWrapperPack<RemoveAdsElementPackConfig, RemoveAdsUserData>
    {
        private const string EVENT_ADD_RIGHT = "falcon.modules.ui.home_shortcut_add_right";
        private const string EVENT_UI_HOME_SHORTCUT_REMOVE = "falcon.modules.ui.home_shortcut_remove";
        private const string EVENT_PROMOTE = "falcon.modules.homeflow.add_promote";
        private const string EVENT_CREATE_PACK_SHOP = "falcon.modules.shop.create_pack_shop";
        private const string EVENT_HIDE_PACK_SHOP = "falcon.modules.shop.hide_pack_shop";
        private const string EVENT_BUY_REMOVE_ADS = "falcon.modules.packs.packsremoveads.buy";
        private const string EVENT_CLOSE_POPUP_NAME = "falcon.modules.core.ui_close_popup_name";
        private const string EVENT_GET_REMOVE_ADS = "falcon.modules.get_remove_ads";
        private const string EVENT_GET_LEVEL = "falcon.modules.core.gamedata.get_level";

        public override string Key => "PacksRemoveAds";

        private readonly Dictionary<string, AssetReference> _packShopAssets = new();

        /// <summary>
        /// Chạy hàm này khi mua gói
        /// </summary>
        public void BuyRemoveAds()
        {
            UserData.timeBuy = 1;
            SaveAndSend();
        }

        protected override void AfterGetData()
        {
            base.AfterGetData();
            var isRemoveAds = GameRequest<bool>.Request(EVENT_GET_REMOVE_ADS);
            if (isRemoveAds && UserData.timeBuy == 0)
            {
                UserData.timeBuy = 1;
                SaveAndSend();
            }
            else if (!isRemoveAds && UserData.timeBuy == 1)
            {
                UserData.timeBuy = 0;
                SaveAndSend();
            }
            if (UserData.timeBuy > 0)
            {
                PacksManager.Remove<WrapperPacksRemoveAds>();
                return;
            }

            foreach (var element in DictConfigs)
            {
                _packShopAssets[element.Key] = new AssetReference($"UIPack_{element.Key}");
            }

            var level = GameRequest<int>.Request(EVENT_GET_LEVEL);
            if (DictConfigs.Values.All(x => x.levelUnlock > level)) return;
            foreach (var element in DictConfigs)
            {
                GameEvent<(AssetReference, string)>.Emit(EVENT_CREATE_PACK_SHOP, (_packShopAssets[element.Key], element.Key));
            }
            GameEvent<string>.Emit(EVENT_ADD_RIGHT, "UIShortcut_RemoveAds");
            GameEvent<(string popupOpen, string key)>.Emit(EVENT_PROMOTE, ("UIPopup_RemoveAds", "Remove_Ads_Bundle"));
        }

        protected override void OnChangeData()
        {
            base.OnChangeData();
            if (UserData.timeBuy > 0)
            {
                PacksManager.Remove<WrapperPacksRemoveAds>();
                foreach (var element in DictConfigs)
                {
                    GameEvent<string>.Emit(EVENT_HIDE_PACK_SHOP, element.Key);
                }

                GameEvent<string>.Emit(EVENT_UI_HOME_SHORTCUT_REMOVE, "UIShortcut_RemoveAds");
                GameEvent<string>.Emit(EVENT_CLOSE_POPUP_NAME, "UIPopup_RemoveAds");
                GameEvent.Emit(EVENT_BUY_REMOVE_ADS);
            }
        }
    }
}