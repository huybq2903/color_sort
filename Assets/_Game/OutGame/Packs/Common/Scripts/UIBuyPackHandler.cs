/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-02
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Packs.Core.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using Falcon.Shared.Common;

namespace Falcon.OutGame.Packs
{
    public class UIBuyPackHandler : MonoBehaviour
    {
        private readonly Dictionary<string, UIItemReward> _dictItem = new();

        private void Awake()
        {
            var uiRewards = GetComponentsInChildren<UIItemReward>(true);
            foreach (var reward in uiRewards)
            {
                _dictItem[reward.data.name.ToLower()] = reward;
            }
        }

        private void AfterBuySuccess(ABaseElementPackConfig.Reward[] rewards)
        {
            PacksHandler.OnClosePurchaseSuccess += AnimReward;
        }

        [Button]
        private void AnimReward()
        {
            var rws = new List<(string, int)>();

            foreach (var item in _dictItem)
            {
                if (item.Value.data.amount == 0) continue;
                rws.Add((item.Key, item.Value.data.amount));
            }

            GameEvent<(string, int)[]>.Emit(GameKeys.REWARD_ENTRY_SPAWN, rws.ToArray());
            // GameEvent<(List<(string id, int value)> rewards, string title, string description)>.Emit("falcon.modules.ui.reward_entry_list", (rws, "test", "test"));
            // GameEvent<(List<(string id, int value)> rewards, string title, string description, Transform chest)>.Emit("falcon.modules.ui.reward_entry_chest", (rws, "test", "test", _dictItem["gold"].transform));
        }
    }

    public static class PacksHandler
    {
        public static event Action OnClosePurchaseSuccess;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            GameEvent.Register(GameKeys.PURCHASE_CLOSE_SUCCESS, OnCloseSuccess, null);
        }

        private static void OnCloseSuccess()
        {
            OnClosePurchaseSuccess?.Invoke();
            OnClosePurchaseSuccess = null;
        }
    }
}
