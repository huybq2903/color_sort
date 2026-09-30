/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-30
 */

using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.Utils.Time.Runtime;
using Falcon.Modules.Packs.Core.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Falcon.Modules.Packs.PacksFailOffer.Runtime
{
    /// <summary>
    /// Wrapper quản lý các gói fail offer
    /// </summary>
    public class WrapperPacksFailOffer : ABaseWrapperPack<FailOfferElementPackConfig, FailOfferUserData>
    {
        private const string EVENT_CREATE_PACK_SHOP = "falcon.modules.shop.create_pack_shop";
        private const string EVENT_HIDE_PACK_SHOP = "falcon.modules.shop.hide_pack_shop";
        public override string Key => "PacksFailOffer";

        protected override void AfterGetData()
        {
            base.AfterGetData();
            UserData.timeBuy ??= new Dictionary<string, int>();
            UserData.coolDown ??= new Dictionary<string, long>();
            UserData.timeEnd ??= new Dictionary<string, long>();
            foreach (var element in DictConfigs)
            {
                if (GetStatePack(element.Key) == StatePack.COOLDOWN || 
                    (element.Value.limitBuy > 0 && 
                     UserData.timeBuy.GetValueOrDefault(element.Key) >= element.Value.limitBuy))
                {
                    GameEvent<string>.Emit(EVENT_HIDE_PACK_SHOP, element.Key);
                    continue;
                }
                GameEvent<(AssetReference, string)>.Emit(EVENT_CREATE_PACK_SHOP, (new AssetReference("UIPack_FailOffer"), element.Key));
            }
        }

        protected override void OnChangeData()
        {
            base.OnChangeData();
            foreach (var element in DictConfigs)
            {
                if (GetStatePack(element.Key) == StatePack.COOLDOWN ||
                    (element.Value.limitBuy > 0 && 
                     UserData.timeBuy.GetValueOrDefault(element.Key) >= element.Value.limitBuy))
                {
                    GameEvent<string>.Emit(EVENT_HIDE_PACK_SHOP, element.Key);
                }
            }
        }

        /// <summary>
        /// Gọi khi mua gói, cộng thêm số lần mua nếu gói đó có limit
        /// </summary>
        /// <param name="idPack"></param>
        public void BuyOffer(string idPack)
        {
            var config = DictConfigs.GetValueOrDefault(idPack);
            if (config is not { limitBuy: > 0 }) return;

            UserData.timeBuy.TryAdd(idPack, 0);
            UserData.timeBuy[idPack]++;
            SaveAndSend();
        }

        /// <summary>
        /// Gọi khi hiển thị gói, tính toán thời gian hiện gói và cooldown
        /// </summary>
        /// <param name="idPack"></param>
        public void ShowOffer(string idPack)
        {
            var config = DictConfigs.GetValueOrDefault(idPack);
            if (config == null || GetStatePack(idPack) != StatePack.WAIT_TO_SHOW) return;
            
            var durationShow = Mathf.Max(config.durationShow, 0);
            var coolDown = Mathf.Max(config.coolDown, 0);
            var isDirty = false;
            if (durationShow > 0)
            {
                var dateTimeEnd = TimeUtils.UTCNow.AddSeconds(durationShow);
                UserData.timeEnd[idPack] = TimeUtils.GetTimestampInSecondsOf(dateTimeEnd);
                isDirty = true;
            }
            
            if (durationShow + coolDown > 0)
            {
                var dateTimeCd = TimeUtils.UTCNow.AddSeconds(durationShow + coolDown);
                UserData.coolDown[idPack] = TimeUtils.GetTimestampInSecondsOf(dateTimeCd);
                isDirty = true;
            }

            if (isDirty) SaveAndSend();
        }

        private StatePack GetStatePack(string idPack)
        {
            var config = DictConfigs.GetValueOrDefault(idPack);
            if (config == null)
            {
                return StatePack.NULL;
            }

            var now = TimeUtils.GetCurrentTimestampInSecondsUTC();
            var timeEnd = UserData.timeEnd.GetValueOrDefault(idPack, -1);
            var coolDown = UserData.coolDown.GetValueOrDefault(idPack, -1);

            if (now < timeEnd) return StatePack.SHOWING;

            if (now < coolDown) return StatePack.COOLDOWN;
            
            return StatePack.WAIT_TO_SHOW;

        }

        private enum StatePack
        {
            NULL,
            SHOWING,
            COOLDOWN,
            WAIT_TO_SHOW,
        }
    }
}