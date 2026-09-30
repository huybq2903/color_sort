/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-13
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Falcon.Modules.Shop.Runtime
{
    /// <summary>
    /// Quản lý toàn bộ cấu hình shop, xử lý khởi tạo và lưu.
    /// </summary>
    public static class ShopManager
    {
        public static ShopConfig shopConfig;
        public static MiniShopConfig miniShopConfig;

        internal static readonly Dictionary<string, AssetReference> kDictAsset = new();

        /// <summary>
        /// Custom cho phép module khác sửa hàm Initialize
        /// Gán ở method trước khi AfterSceneLoad
        /// </summary>
        public static Action onInitialize;

        /// <summary>
        /// Custom cho phép module khác sửa dictKeyGroupTitle
        /// để lấy ra tên các group title, có thể gán null để k cần title
        /// </summary>
        public static Dictionary<int, Func<string>> dictKeyGroupTitle = new()
        {
            { 1, () => "Special" },
            { 2, () => "Ads" },
            { 3, () => "Bundle" },
            { 4, () => "Currency" },
        };
            
        private static void OnLogin(bool success)
        {
            if (success) new CSShopConfig().Send();
        }
        
        /// <summary>
        /// Khởi tạo shop, lắng nghe login thành công để lấy config từ server.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static async void Initialize()
        {
            try
            {
                GameEvent<(AssetReference asset, string idPack)>.Register(ShopConstant.EVENT_CREATE_PACK_SHOP, CreatePack, null);
                GameEvent<string>                               .Register(ShopConstant.EVENT_HIDE_PACK_SHOP, HidePack, null);
                GameEvent<bool>                                 .Register(ShopConstant.EVENT_LOGIN, OnLogin, null);
            
                await Task.Yield();
                var handle = Resources.LoadAsync<SOShopConfig>("SO_FCM_Shop_Config");
                await handle;
                var so = handle.asset as SOShopConfig;
                shopConfig = SaveLoadHandler.Load("Shop_Config", new ShopConfig());
                miniShopConfig = SaveLoadHandler.Load("MiniShop_Config", new MiniShopConfig());

                if (shopConfig.elementConfigs is not { Length: > 0 } && so)
                {
                    shopConfig = so.shopConfig;
                }
            
                if (miniShopConfig.elementConfigs is not { Length: > 0 } && so)
                {
                    miniShopConfig = so.miniShopConfig;
                }
            
                onInitialize?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal static void SaveShopConfig(ShopConfig config)
        {
            if (config is not { elementConfigs: { Length: > 0 } }) return;
            shopConfig = config;
            SaveLoadHandler.Save("Shop_Config", shopConfig);
        }
        
        internal static void SaveMiniShopConfig(MiniShopConfig config)
        {
            if (config is not { elementConfigs: { Length: > 0 } }) return;
            miniShopConfig = config;
            SaveLoadHandler.Save("MiniShop_Config", miniShopConfig);
        }
        
        private static void CreatePack((AssetReference asset, string idPack) pack)
        {
            kDictAsset[pack.idPack] = pack.asset;
        }

        private static void HidePack(string idPack)
        {
            kDictAsset.Remove(idPack);
        }
    }
}