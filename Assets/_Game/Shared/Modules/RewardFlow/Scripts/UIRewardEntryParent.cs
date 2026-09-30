/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-07
 */

using Falcon.Helpers.EventBus;
using UnityEngine;

namespace Falcon.Shared.RewardFlow
{
    public class UIRewardEntryParent : MonoBehaviour
    {
        public string where;

        private void OnEnable()
        {
            GameRequest<Transform>.Register($"falcon.reward.entry_parent_{where}", OnEntryParent, this);
        }

        private void OnDisable()
        {
            GameRequest<Transform>.Unregister($"falcon.reward.entry_parent_{where}");
        }

        private Transform OnEntryParent() => transform;
    }
}