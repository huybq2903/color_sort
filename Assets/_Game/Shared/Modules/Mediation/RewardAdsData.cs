/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-05
 */

using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using Falcon.Shared.Common.Time;
using Falcon.Shared.Common;

namespace Falcon.Shared.Mediation
{
    [FGameDataType("reward_ads_game")]
    public class RewardAdsData : FGameData<RewardAdsData>
    {
        public long day;
        public Dictionary<string, int> numAdsToBuyBoosters = new();
        public int numAdsToRevive;

        public void Initialized()
        {
            CheckNewDay();
            AccountManager.Instance.OnUpdateFromServer += CheckNewDay;
            GameEvent.Register(GameKeys.WIN, OnWinGame, null);
        }

        private void OnWinGame()
        {
            Instance.numAdsToRevive = 0;
            Instance.Save();
        }

        private void CheckNewDay(ClientData obj) => CheckNewDay();

        private void CheckNewDay()
        {
            var currentDay = WrapperTime.CurrentDay;

            if (Instance.day != currentDay)
            {
                Instance.day = currentDay;
                Instance.numAdsToBuyBoosters.Clear();
                Instance.Save();
            }
        }
    }
}