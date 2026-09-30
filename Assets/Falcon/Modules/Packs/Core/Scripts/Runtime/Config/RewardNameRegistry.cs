using UnityEngine;

namespace Falcon.Modules.Packs.Core.Runtime
{
    [CreateAssetMenu(fileName = "RewardNameRegistry", menuName = "Pack/Reward Name Registry")]
    public class RewardNameRegistry : ScriptableObject
    {
        public string[] names;
    }
}
