/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-09-29
 */

using System;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.InGame.Log;
using Falcon.InGame.UI;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.BaseInGame;
using Falcon.Shared.Common;

namespace Falcon.InGame.Classic
{
    /// <summary>Default mode: owns the win / temp-lose popup flow.</summary>
    [GameMode(GameModeKeys.CLASSIC)]
    public class ModeClassic : AModeGame, ILevelBeforeSpawn, ILevelAfterSpawn
    {
        private int _level;
        private int _difficulty;
        private IDisposable[] _subscriptions;

        [Inject] private readonly InGameLogManager _logManager;
        [Inject] private readonly AWinLoseManager _winLoseManager;
        [Inject] private readonly ALevelManager _levelManager;

        public override void OnEnter()
        {
            _level = GameRequest<int>.Request(GameKeys.GET_LEVEL);
            _difficulty = GameRequest<int, int>.Request(GameKeys.GET_LEVEL_DIFFICULTY, _level);

            var tempLoseFlow = Flow.Of(GameKeys.TEMP_LOSE_FLOW);
            _subscriptions = new[]
            {
                Flow.Of(GameKeys.WIN_FLOW).Add(OnShowPopupWin),
                tempLoseFlow.Add(OnShowPopupContinue),
                tempLoseFlow.Add(OnShowPopupLose, 1000), // after DoGiveUp (999)
            };
        }

        public override void OnExit()
        {
            if (_subscriptions == null) return;
            foreach (var subscription in _subscriptions) subscription.Dispose();
        }

        public UniTask BeforeSpawn()
        {
            Hud.GetComponent<InGameHUD>().SetData(_level);
            return UniTask.CompletedTask;
        }

        public UniTask AfterSpawn()
        {
            if (!_levelManager.IsResume && _difficulty > 0)
                UIWrapper.OpenPopup("UIPopupWarningDifficulty", p => p.GetComponent<UIPopupWarningDifficulty>().UpdateUI(_difficulty));
            return UniTask.CompletedTask;
        }

        private void OnShowPopupWin()
        {
            var levelStartAdsWin = GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "level_start_ads_win");
            UIWrapper.OpenPopup("UIPopupWin", p => p.GetComponent<UIPopupWin>().SetData(_level, _difficulty, levelStartAdsWin <= _level));
        }

        private void OnShowPopupContinue(Action next)
        {
            var levelStartAdsRevive = GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "level_start_ads_revive");
            var goldToRevive = GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, "price_revive");
            var numRevive = _logManager.Stats.amountMoreTime;
            var currentGold = ResourceCollector.Instance.GetResourceValueIntInCollector("gold");

            var isCanByAds = _level >= levelStartAdsRevive && numRevive == 0 && currentGold < goldToRevive;
            UIWrapper.OpenPopup("UIPopupContinue", p =>
            {
                var popup = p.GetComponent<UIPopupContinue>();
                popup.OnGiveUp = next;
                popup.OnRevive = _winLoseManager.DoRevive;
                popup.SetData(isCanByAds, goldToRevive);
            });
        }

        private void OnShowPopupLose()
        {
            UIWrapper.OpenPopup("UIPopupLose");
        }
    }
}
