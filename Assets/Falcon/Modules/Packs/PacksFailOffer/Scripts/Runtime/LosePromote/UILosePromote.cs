/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Falcon.Modules.Packs.PacksFailOffer.Runtime
{
    /// <summary>
    /// UI banner trong popup thua game
    /// </summary>
    public class UILosePromote : MonoBehaviour
    {
        private const string EVENT_HIDE_PACK_SHOP = "falcon.modules.shop.hide_pack_shop";
        private const string EVENT_BUY_SUCCESS = "falcon.modules.packs.buy_success";
        private const string EVENT_CONTINUE_GAME = "falcon.modules.shoplosegame.continue";

        private UIBannerGroupPack _groupPack;
        private readonly Dictionary<int, string> _dictPosition = new();
        private bool _createdPack;
        
        public UnityEvent onCreatePackDone;

        private void Awake()
        {
            _groupPack = GetComponentInChildren<UIBannerGroupPack>();
            _groupPack.Initialize();
        }

        private void OnEnable()
        {
            GameEvent<string>.Register(EVENT_HIDE_PACK_SHOP, HidePack, this);
            GameEvent<string>.Register(EVENT_BUY_SUCCESS, OnBuyPack, this);
            CreatePacks();
        }

        private async void CreatePacks()
        {
            await Task.Delay(100);
            if (_createdPack)
            {
                onCreatePackDone?.Invoke();
                return;
            }

            _dictPosition.Clear();
            foreach (var config in LosePromoteManager.configs) CreatePack(config);
            _createdPack = true;

            if (_groupPack.QuantityPackShowing == 0)
            {
                gameObject.SetActive(false);
            }
            onCreatePackDone?.Invoke();
        }

        private void OnDisable()
        {
            GameEvent<string>.Unregister(EVENT_HIDE_PACK_SHOP, HidePack, this);
            GameEvent<string>.Unregister(EVENT_BUY_SUCCESS, OnBuyPack, this);
        }

        /// <summary>
        /// Tạo pack từ config
        /// </summary>
        /// <param name="config"></param>
        private void CreatePack(LosePromoteConfig config)
        {
            var idPack = config.idPack;
            var asset = GetAssetById(idPack);
            if (asset == null || _dictPosition.Count >= LosePromoteManager.quantityShow || !_dictPosition.TryAdd(config.priority, null))
            {
                return;
            }

            if (_groupPack.Add(idPack, asset))
            {
                _dictPosition[config.priority] = idPack;
            }
            else
            {
                _dictPosition.Remove(config.priority);
            }
            SortElement();
        }

        private void HidePack(string idPack)
        {
            _groupPack.Remove(idPack);
            var lsPriority = _dictPosition.Keys.ToArray();
            foreach (var priority in lsPriority)
            {
                if (_dictPosition[priority] == idPack)
                {
                    _dictPosition.Remove(priority);
                }
            }

            SortElement();

            if (_groupPack.QuantityPackShowing == 0)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnBuyPack(string idPack)
        {
            if (GetConfigById(idPack) != null && GetConfigById(idPack).continueGame == 1)
                GameEvent.Emit(EVENT_CONTINUE_GAME);
        }

        private void SortElement()
        {
            foreach (var (priority, idPack) in _dictPosition)
            {
                if (idPack == null) continue;

                var (goPack, toggle) = _groupPack.GetItem(idPack);

                if (!goPack) return;
                goPack.SetSiblingIndex(priority);
                toggle.transform.SetSiblingIndex(priority);
            }

            _groupPack.UpdateScroll();
        }

        /// <summary>
        /// Tìm config theo idPack
        /// </summary>
        private LosePromoteConfig GetConfigById(string idPack)
        {
            return LosePromoteManager.configs.FirstOrDefault(e => e.idPack == idPack);
        }

        private AssetReference GetAssetById(string idPack)
        {
            return LosePromoteManager.kDictAsset.GetValueOrDefault(idPack);
        }
    }
}
