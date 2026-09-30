/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-28
 */

using System;
using Falcon.Helpers.EventBus;
using Falcon.Helpers.FReflection;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.Common;
using ULoc;

namespace Falcon.Shared.BaseBooster
{
    public abstract class ABoosterLogic : IFReflection, IDisposable
    {
        protected ABoosterManager _manager;
        protected ABoosterResource _resource;

        public abstract string Type { get; }

        public virtual int LevelUnlock => BoosterConfig.LevelUnlock(Type);

        public virtual bool NeedUnlockTutorial => GameRequest<int>.Request(GameKeys.GET_LEVEL) == LevelUnlock && _resource.IsFree;

        public virtual void Initialize(ABoosterManager manager)
        {
            _manager = manager;
            _resource = ResourceCollector.Instance.GetResourceInCollector(Type) as ABoosterResource;
        }

        public virtual bool BeforeExecute()
        {
            if (DataTempExtensions<bool>.Get("from_editor"))
                return true;

            if (GameRequest<int>.Request(GameKeys.GET_LEVEL) < LevelUnlock)
            {
                GameEvent<string>.Emit(GameKeys.TOAST_OPEN, ULocGame.unlock_level.Param($"{LevelUnlock}"));
                return false;
            }
            else if (_resource.Quantity == 0 && !_resource.IsFree)
            {
                UIWrapper.OpenPopup("UIPopupBuyBooster", popup =>
                {
                    var levelStartAds = GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "level_start_ads_buy_booster");

                    var uiPopup = popup.GetComponent<UIPopupBuyBooster>();
                    uiPopup.BoosterType = Type;
                    uiPopup.UpdateUI(levelStartAds <= GameRequest<int>.Request(GameKeys.GET_LEVEL));
                });
                return false;
            }

            return true;
        }

        public abstract void Execute();

        public virtual void AfterExecute()
        {
            if (DataTempExtensions<bool>.Get("from_editor"))
                return;
            GameEvent<string>.Emit(GameKeys.AFTER_BOOSTER_USED, Type);
            if (_resource.IsFree)
                _resource.IsFree = false;
            else
                ResourceCollector.Instance.ResourceRemove(Type, 1, null, $"use_{Type}_in_game");

            ResourceCollector.Instance.SaveAndUpdateServerOfResourceData(Type);
            GameEvent.Emit($"falcon.game.ui_{Type}_update");
        }

        public virtual void Dispose() { }
    }
}
