// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-04-22

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Shared.Common;
using UnityEngine;
using Falcon.Modules.UI.Menu.Runtime;

namespace Falcon.OutGame.Core
{
    public class WrapperGoldHome : IInitialize
    {
        private const float DURATION_FLY = 2.5f;

        private int _deltaGold;
        private GoldResource _goldResource;

        public int PrevGold { get; private set; }

        public void OnInitialize()
        {
            _goldResource = ResourceCollector.Instance.GetResourceInCollector<GoldResource>("gold");
            _goldResource.OnChanged += OnGoldChanged;
            GameEvent.Register(GameKeys.LOAD_SCENE_LEVEL_COMPLETE, OnStartGame, null);
            GameEvent.Register(MenuConst.EVENT_UI_HOME_LOAD_COMPLETE, OnLoadHome, null);
            Flow.Of(GameKeys.HOME_FLOW).Add(ClaimGold, HomeFlowRunner.ORDER_RESOURCE_CLAIM);
        }

        private void OnGoldChanged(int amount, string data)
        {
            if (data == "win")
            {
                _deltaGold += amount;
            }
        }

        private void OnLoadHome()
        {
            PrevGold = _goldResource.GetInt - _deltaGold;
        }

        private async UniTask ClaimGold(CancellationToken ct)
        {
            if (_deltaGold <= 0) return;
            GameEvent<(int, Vector3?, string)>.Emit(GameKeys.REWARD_ENTRY_SPAWN_GOLD, (_deltaGold, null, "home"));
            _deltaGold = 0;
            await UniTask.Delay(TimeSpan.FromSeconds(DURATION_FLY), cancellationToken: ct);
        }

        private void OnStartGame()
        {
            _deltaGold = 0;
            PrevGold = _goldResource.GetInt;
        }
    }
}