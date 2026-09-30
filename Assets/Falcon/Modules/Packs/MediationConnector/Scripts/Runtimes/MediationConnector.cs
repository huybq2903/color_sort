/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-08-14
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using UnityEngine;

namespace Falcon.Modules.Packs.MediationConnector.Runtimes
{
    public static class MediationConnector
    {
        private const string EVENT_BUY_REMOVE_ADS = "falcon.modules.packs.packsremoveads.buy";
        private const string EVENT_GET_REMOVE_ADS = "falcon.modules.get_remove_ads";
        [RuntimeInitializeOnLoadMethod]
        private static void RegisterRemoveAds()
        {
            GameEvent.Register(EVENT_BUY_REMOVE_ADS, OnBuyRemoveAds, null);
            GameRequest<bool>.Register(EVENT_GET_REMOVE_ADS, GetRemoveAds);
        }

        private static void OnBuyRemoveAds()
        {
            MediationManager.Instance.SetRemoveAds(true);
        }

        private static bool GetRemoveAds()
        {
            return GameData4RemoveAds.Instance.removeAds;
        }
    }
}