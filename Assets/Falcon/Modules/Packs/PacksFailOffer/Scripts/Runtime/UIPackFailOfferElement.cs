/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-01
 */

using System.Collections;
using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.Utils.Time.Runtime;
using Falcon.Modules.Packs.Core.Runtime;
using TMPro;
using UnityEngine;

namespace Falcon.Modules.Packs.PacksFailOffer.Runtime
{
    /// <summary>
    /// UI Pack fail offer, hiện duration nếu có
    /// </summary>
    public class UIPackFailOfferElement : APackElement<WrapperPacksFailOffer>
    {
        private const string EVENT_HIDE_PACK_SHOP = "falcon.modules.shop.hide_pack_shop";
        [SerializeField] private TMP_Text textTag, textDuration, textLimit, textName;
        [SerializeField] private Sprite[] sGoldIcon;
        [SerializeField] private int[] goldMilestones = { 4000, 7000, 14000, 30000, 60000 };

        private GameObject _goTag, _goDuration, _goLimit;

        protected override void Awake()
        {
            base.Awake();
            _goTag = textTag.transform.parent.gameObject;
            _goDuration = textDuration.transform.parent.gameObject;
            _goLimit = textLimit.transform.parent.gameObject;
        }

        protected override void OnBuySuccess()
        {
            base.OnBuySuccess();
            _wrapper.BuyOffer(idPack);
        }

        protected override void SetupUI()
        {
            base.SetupUI();
            if (_config is not FailOfferElementPackConfig configFailOffer) return;

            _wrapper.ShowOffer(idPack);
            UpdateLimit(configFailOffer);
            UpdateTag(configFailOffer);
            UpdateDuration();
            textName.text = configFailOffer.name.Localize();
            
            if (_dictItem.TryGetValue("gold", out var itemGold))
            {
                itemGold.Icon.sprite = sGoldIcon[GetGoldIconIndex(itemGold.data.amount)];
                itemGold.Icon.SetNativeSize();
            }
        }

        private void OnEnable()
        {
            UpdateDuration();
        }

        private void UpdateLimit(FailOfferElementPackConfig configFailOffer)
        {
            if (configFailOffer.limitBuy > 0)
            {
                _goLimit.SetActive(true);
                var timeBuy = _wrapper.UserData.timeBuy.GetValueOrDefault(idPack);
                textLimit.text = $"{configFailOffer.limitBuy - timeBuy}/{configFailOffer.limitBuy}";
            }
            else
            {
                _goLimit.SetActive(false);
            }
        }

        private void UpdateTag(FailOfferElementPackConfig configFailOffer)
        {
            if (!string.IsNullOrEmpty(configFailOffer.tag))
            {
                _goTag.SetActive(true);
                textTag.text = $"{configFailOffer.tag}";
            }
            else
            {
                _goTag.SetActive(false);
            }
        }

        private void UpdateDuration()
        {
            var timeEnd = _wrapper.UserData.timeEnd.GetValueOrDefault(idPack, -1);
            if (timeEnd > 0)
            {
                _goDuration.SetActive(true);
                if (_coDuration != null) StopCoroutine(_coDuration);
                _coDuration = CoDuration(timeEnd);
                StartCoroutine(_coDuration);
            }
            else
            {
                _goDuration.SetActive(false);
            }
        }

        private IEnumerator _coDuration;

        private IEnumerator CoDuration(long timeEnd)
        {
            var duration = timeEnd - TimeUtils.GetCurrentTimestampInSecondsUTC();
            while (duration > 0)
            {
                textDuration.text = duration.FormatSecondsCoolDown();
                yield return new WaitForSeconds(1f);
                duration = timeEnd - TimeUtils.GetCurrentTimestampInSecondsUTC();
            } 
            GameEvent<string>.Emit(EVENT_HIDE_PACK_SHOP, idPack);
        }
        
        private int GetGoldIconIndex(int amountGold)
        {
            var index = 0;
            while (index < goldMilestones.Length && amountGold > goldMilestones[index])
            {
                index++;
            }
            return index;
        }
    }
}