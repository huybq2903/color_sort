/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-24
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Packs.Core.Runtime
{
    public abstract class APackElement<W> : MonoBehaviour where W : class, IWrapperPack
    {
        protected const string EVENT_HIDE_PACK_SHOP = "falcon.modules.shop.hide_pack_shop";

        [SerializeField] protected string idPack;
        [SerializeField] protected TMP_Text textPrice;
        [Tooltip("Bật thì pack sẽ tự setup với idPack đã điền ở trên khi vào awake, tắt thì sẽ phải tự setup bằng code")]
        [SerializeField] protected bool isSetupInAwake;
        [SerializeField] protected string placement = "unknown";

        protected W _wrapper;
        protected ABaseElementPackConfig _config;
        protected UIItemReward[] _uiRewards;
        protected Button _buttonBuy;

        protected readonly Dictionary<string, UIItemReward> _dictItem = new();

        protected virtual void Awake()
        {
            _uiRewards = GetComponentsInChildren<UIItemReward>(true);
            foreach (var reward in _uiRewards)
            {
                _dictItem[reward.data.name.ToLower()] = reward;
            }

            _wrapper = PacksManager.Get<W>();
            _buttonBuy = GetComponentInChildren<Button>();
            _buttonBuy.onClick.AddListener(Purchase);

            if (isSetupInAwake)
            {
                Setup(idPack);
            }
        }

        public void SetPlacement(string placementNew)
        {
            placement = placementNew;
        }

        /// <summary>
        /// Gọi bằng reflection để set idPack
        /// </summary>
        /// <param name="idPackNew"></param>
        public void Setup(string idPackNew)
        {
            idPack = idPackNew;
            _config = _wrapper?.Config?.elements?.FirstOrDefault(p => p.idPack == idPack);
            if (_config == null)
            {
                GameEvent<string>.Emit(EVENT_HIDE_PACK_SHOP, idPack);
                return;
            }

            SetupUI();
        }

        /// <summary>
        /// Chỉ gọi 1 lần ban đầu khi Setup id pack
        /// </summary>
        protected virtual void SetupUI()
        {
            SetUpPrice();
            UpdateReward(_config.rewards);
        }

        protected virtual async void SetUpPrice()
        {
            try
            {
                var taskPrice = GameRequest<string, Task<string>>.Request(PacksConstant.EVENT_GET_LOCALIZED_PRICE, _config.productId);
                await taskPrice;
                textPrice.text = taskPrice.Result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected virtual void OnBuySuccess()
        {
            if (_config.rewards.Length <= 0) return;
            var ls = new (string name, int amount, string data)[_config.rewards.Length];
            for (var i = 0; i < _config.rewards.Length; i++)
            {
                ls[i] = (_config.rewards[i].name, _config.rewards[i].amount, _config.rewards[i].data);
            }

            GameEvent<string>.Emit(PacksConstant.EVENT_BUY_SUCCESS, idPack);
            GameEvent<ABaseElementPackConfig>.Emit(PacksConstant.EVENT_BUY_SUCCESS_ADD_RESOURCES, _config);
            SendMessage("AfterBuySuccess", _config.rewards, SendMessageOptions.DontRequireReceiver);
        }

        protected virtual void OnLocalize()
        {
        }

        protected virtual void UpdateReward(ABaseElementPackConfig.Reward[] lsData)
        {
            foreach (var reward in _uiRewards)
            {
                reward.gameObject.SetActive(false);
            }

            foreach (var data in lsData)
            {
                if (!_dictItem.TryGetValue(data.name.ToLower(), out var uiItem)) continue;
                uiItem.gameObject.SetActive(true);
                uiItem.data = data;
                uiItem.SetText();
            }
        }

        protected virtual void Purchase()
        {
            GameEvent<(string, Action, Action, string, string)>.Emit(PacksConstant.EVENT_PURCHASE,
                (_config.productId, OnBuySuccess, null, placement, idPack));
        }

        protected virtual void OnEnable()
        {
            LocalizationManager.OnLocalizeEvent += OnLocalize;
            OnLocalize();
        }

        protected virtual void OnDisable()
        {
            LocalizationManager.OnLocalizeEvent -= OnLocalize;
        }
    }
}