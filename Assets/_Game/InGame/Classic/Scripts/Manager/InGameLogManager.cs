/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.BigData;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Level.Core;
using Falcon.Shared.BaseInGame;
using UnityEngine;
using Falcon.Shared.Common;

namespace Falcon.InGame.Log
{
    /// <summary>
    /// Gom toàn bộ log analytics của một ván chơi về một chỗ: các manager khác chỉ emit event, không tự log.
    /// </summary>
    public class InGameLogManager : MonoBehaviour, ISubManager
    {
        [Inject] private ALevelManager _levelManager;

        private StatsRuntime _stats;
        private GoldResource _goldResource;
        private int _level;
        private float _startTime;

        /// <summary>Tổng điểm để tính % tiến độ khi thua. Game con override, mặc định 1 để khỏi chia 0.</summary>
        protected virtual int TotalScore => 1;

        /// <summary>
        /// Lazy vì LevelRuntime chỉ có sau khi ALevelManager.Initialized() chạy,
        /// mà thứ tự Initialized giữa các ISubManager phụ thuộc component order trên scene.
        /// </summary>
        public StatsRuntime Stats
        {
            get
            {
                if (_levelManager.LevelRuntime == null)
                {
                    // Runtime đã clear sau win/lose (GameKeys.CLEAR_LEVEL_RUNTIME), cache stale phải bỏ
                    _stats = null;
                    return null;
                }
                if (_stats != null) return _stats;

                _stats = _levelManager.LevelRuntime.GetOrCreateProperty<StatsRuntime>();
                _startTime -= _stats.durationPlayed; // resume: trừ lại thời gian đã chơi trước đó
                return _stats;
            }
        }

        public void Initialized()
        {
            _startTime = Time.realtimeSinceStartup;
            _level = GameRequest<int>.Request(GameKeys.GET_LEVEL);

            GameEvent.Register(GameKeys.LOAD_SCENE_LEVEL_COMPLETE, OnLevelReady, this);
            GameEvent.Register(GameKeys.SETUP_LEVEL_COMPLETE, OnLevelStart, this);
            GameEvent.Register(GameKeys.WIN, OnWin, this);
            GameEvent.Register(GameKeys.LOSE, OnLose, this);
            GameEvent.Register(GameKeys.REVIVE, OnRevive, this);
            GameEvent<string>.Register(GameKeys.AFTER_BOOSTER_USED, OnBoosterUsed, this);
            GameEvent<int>.Register(GameKeys.LOG_SCORE, OnAddScore, this);

            _goldResource = ResourceCollector.Instance.GetResourceInCollector<GoldResource>("gold");
            _goldResource.OnChanged += OnChangeGold;
        }

        private void OnDisable()
        {
            GameEvent.Unregister(GameKeys.LOAD_SCENE_LEVEL_COMPLETE, OnLevelReady, this);
            GameEvent.Unregister(GameKeys.SETUP_LEVEL_COMPLETE, OnLevelStart, this);
            GameEvent.Unregister(GameKeys.WIN, OnWin, this);
            GameEvent.Unregister(GameKeys.LOSE, OnLose, this);
            GameEvent.Unregister(GameKeys.REVIVE, OnRevive, this);
            GameEvent<string>.Unregister(GameKeys.AFTER_BOOSTER_USED, OnBoosterUsed, this);
            GameEvent<int>.Unregister(GameKeys.LOG_SCORE, OnAddScore, this);

            if (_goldResource != null)
                _goldResource.OnChanged -= OnChangeGold;
        }

        private void OnLevelReady()
        {
            FLevelManager.Instance.OnLevelReady();
            new FFilteredFunnelLog(
                "level_funnel_drop_1",
                $"start_level_{_level}",
                2 * _level - 1,
                _level
            ).Send();
        }

        private void OnLevelStart() => FLevelManager.Instance.OnLevelStart();

        // Chỉ đếm chiều tiêu vàng (amount âm), chiều cộng vàng bỏ qua.
        private void OnChangeGold(int amount, string data)
        {
            if (amount >= 0) return;

            var stats = Stats;
            if (stats == null) return;

            stats.coinSpend += -amount;
            _levelManager.LevelRuntime.SetProperty(stats);
        }

        private void OnRevive()
        {
            var stats = Stats;
            if (stats == null) return;

            stats.amountMoreTime++;
            _levelManager.LevelRuntime.SetProperty(stats);
        }

        private void OnBoosterUsed(string boosterType)
        {
            var stats = Stats;
            if (stats == null) return;

            stats.numUsedBoosters.TryGetValue(boosterType, out var count);
            stats.numUsedBoosters[boosterType] = count + 1;
            _levelManager.LevelRuntime.SetProperty(stats);
        }

        /// <summary>Gameplay emit điểm cộng thêm, cộng dồn vào stats để resume không đếm lại.</summary>
        private void OnAddScore(int amount)
        {
            var stats = Stats;
            if (stats == null) return;

            stats.score += amount;
            _levelManager.LevelRuntime.SetProperty(stats);
        }

        private void OnWin()
        {
            new FFilteredFunnelLog(
                "level_funnel_drop_1",
                $"complete_level_{_level}",
                2 * _level,
                _level
            ).Send();

            var difficulty = GameRequest<int, int>.Request(GameKeys.GET_LEVEL_DIFFICULTY, _level);
            var gold = GameRequest<string, int>.Request(GameKeys.GET_REMOTE_CONFIG, $"goldWin_{difficulty}");

            ResourceCollector.Instance.ResourceAdd("gold", gold, "win", "win game");
            ResourceCollector.Instance.SaveAndUpdateServerOfResourceData("gold");
            Log(true);
        }

        private void OnLose() => Log(false);

        private void Log(bool win)
        {
            var stats = Stats;
            if (stats != null)
            {
                stats.durationPlayed = (int)(Time.realtimeSinceStartup - _startTime);
                _levelManager.LevelRuntime.SetProperty(stats);
            }

            // numberMove = 0: template chưa đếm số nước đi.
            FLevelManager.Instance.OnLevelResult(
                win: win,
                time: stats?.durationPlayed ?? 0,
                booster2Number: stats?.numUsedBoosters,
                score: stats?.score ?? 0,
                totalScore: TotalScore,
                coinSpend: stats?.coinSpend ?? 0,
                numberBuyMoreTime: stats?.amountMoreTime ?? 0,
                numberMove: 0);

            GameEvent.Emit(GameKeys.CLEAR_LEVEL_RUNTIME);
        }
    }
}
