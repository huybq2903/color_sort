using UnityEngine;

namespace Falcon.Shared.BaseEvents
{
    [CreateAssetMenu(fileName = "EventRewardNameRegistry", menuName = "Event/Reward Name Registry")]
    public class EventRewardNameRegistry : ScriptableObject
    {
        public string[] names;
    }
}
