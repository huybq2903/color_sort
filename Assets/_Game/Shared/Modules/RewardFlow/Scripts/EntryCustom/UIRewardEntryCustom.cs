/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-09
 */

using System;
using Falcon.Helpers.EventBus;
using UnityEngine;

namespace Falcon.Shared.RewardFlow
{
    public class UIRewardEntryCustom : RewardEntry<RewardCustomData>
    {
        private const string EVENT_ENTRY_CUSTOM_DONE = "falcon.modules.ui.reward_entry_custom.done";

        private void OnEnable()
        {
            GameEvent.Register(EVENT_ENTRY_CUSTOM_DONE, OnDone, this);
        }

        public override void Open()
        {
            if (Data.obj)
            {
                Data.obj.SetParent(transform);
                Data.obj.SendMessage("Show", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                OnDone();
            }
        }
        
        private void OnDone()
        {
            OnDispose?.Invoke();
            OnNext?.Invoke();
        }

        private void OnDisable()
        {
            GameEvent.Unregister(EVENT_ENTRY_CUSTOM_DONE, OnDone, this);
        }
    }

    public class RewardCustomData : IRewardEntryData
    {
        public Transform obj;
    }
}