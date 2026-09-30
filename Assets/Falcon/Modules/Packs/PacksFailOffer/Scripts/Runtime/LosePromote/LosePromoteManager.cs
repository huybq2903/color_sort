/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.SaveLoad.Runtime;
using Falcon.Modules.Packs.Core.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Falcon.Modules.Packs.PacksFailOffer.Runtime
{
    /// <summary>
    /// Class quản lý, lưu config, asset các pack sẽ xuất hiện trong banner các gói bán ở dưới popup thua game
    /// </summary>
    public static class LosePromoteManager
    {
        public static Action onInitialize;
        public static int quantityShow;
        public static List<LosePromoteConfig> configs;
        
        internal static readonly Dictionary<string, AssetReference> kDictAsset = new();

        private static SOLosePromoteConfig _soConfig;
        
        private const string EVENT_CREATE_PACK_SHOP = "falcon.modules.shop.create_pack_shop";
        private const string EVENT_HIDE_PACK_SHOP = "falcon.modules.shop.hide_pack_shop";

        
        /// <summary>
        /// Lấy config từ bộ nhớ, nếu chưa có thì lấy mặc định từ SO
        /// Đăng kí các sự kiện event bus để lấy asset pack từ các module
        /// Khi đăng nhập thành công gửi CS để lấy config động từ server
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            _soConfig = Resources.Load<SOLosePromoteConfig>("SO_FCM_LosePromote_Config");
            quantityShow = SaveLoadHandler.Load("quantityShowLosePromote", _soConfig ? _soConfig.quantityShow : 0);
            configs = SaveLoadHandler.Load<List<LosePromoteConfig>>(nameof(LosePromoteConfig));

            if (configs is not { Count: > 0 } && _soConfig)
            {
                configs = _soConfig.configs;
            }

            if (_soConfig && !_soConfig.getQuantityConfigFromCMS)
            {
                quantityShow = _soConfig.quantityShow;
            }
            
            GameEvent<(AssetReference asset, string idPack)>.Register(EVENT_CREATE_PACK_SHOP, CreatePack, null);
            GameEvent<string>                               .Register(EVENT_HIDE_PACK_SHOP, HidePack, null);
            GameEvent<bool>                                 .Register(PacksConstant.EVENT_LOGIN, OnLogin, null);
            
            onInitialize?.Invoke();
        }

        private static void CreatePack((AssetReference asset, string idPack) pack)
        {
            kDictAsset[pack.idPack] = pack.asset;
        }

        private static void HidePack(string idPack)
        {
            kDictAsset.Remove(idPack);
        }

        private static void OnLogin(bool success)
        {
            if (success) new CSGetLosePromoteConfig().Send();
        }

        /// <summary>
        /// Lưu config từ server xuống dưới bộ nhớ
        /// </summary>
        /// <param name="sc"></param>
        internal static void SaveConfig(SCLosePromoteConfig sc)
        {
            if (!_soConfig || _soConfig.getQuantityConfigFromCMS)
            {
                quantityShow = sc.quantityShow;
                SaveLoadHandler.Save("quantityShowLosePromote", quantityShow);
            }
            
            if (sc.configs is not { Count: > 0 }) return;
            configs = sc.configs;
            SaveLoadHandler.Save(nameof(LosePromoteConfig), configs);
        }
    }
}