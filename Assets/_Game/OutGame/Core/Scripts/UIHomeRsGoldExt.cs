/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-13
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Modules.UI.Menu.Runtime;
using Falcon.Shared.RewardFlow;
using Falcon.Shared.Common;

namespace Falcon.OutGame.Core
{
    public class UIHomeRsGoldExt : UIHomeRsGold
    {
        public bool updateGoldOnEnable = true;

        private GoldResource _goldResource;
        private UIResource _uiResource;

        protected override void OnEnable()
        {
            _goldResource = ResourceCollector.Instance.GetResourceInCollector<GoldResource>("gold");
            _uiResource = GetComponent<UIResource>();

            _goldResource.OnChanged += OnChangedGold;
            GameEvent.Register(GameKeys.GOLD_UPDATE, OnGoldUpdate, this);

            btnGetMoreGold.onClick.RemoveAllListeners();
            btnGetMoreGold.onClick.AddListener(() =>
            {
                GameEvent<string>.Emit(Const.EVENT_OPEN_POPUP_NAME, "UIPopupShop");
            });

            if (updateGoldOnEnable)
                base.OnEnable();
        }

        private void OnDisable()
        {
            _goldResource.OnChanged -= OnChangedGold;
            GameEvent.Unregister(GameKeys.GOLD_UPDATE, OnGoldUpdate, this);
        }

        private void OnChangedGold(int amount, string data)
        {
            if (amount > 0) return;
            UpdateUI();
        }

        protected override void UpdateUI()
        {
            base.UpdateUI();
            _uiResource.ReplaceValue((int)_goldResource.Get).DoImmediately();
        }

        private void OnGoldUpdate()
        {
            _uiResource.ReplaceValue((int)_goldResource.Get);
            _uiResource.DoAnim();
        }
    }
}