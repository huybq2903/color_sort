/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
 */

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.Packs.PacksFailOffer.Runtime
{
    [CreateAssetMenu(fileName = "SO_FCM_LosePromote_Config", menuName = "Pack/LosePromoteConfig")]
    public class SOLosePromoteConfig : SerializedScriptableObject
    {
        public bool getQuantityConfigFromCMS;
        public int quantityShow;
        public List<LosePromoteConfig> configs;
    }
    public class LosePromoteConfig
    {
        /// <summary>
        /// Mã duy nhất của pack
        /// </summary>
        public string idPack;
        /// <summary>
        /// Độ ưu tiên, theo thứ tự từ trái sang phải
        /// </summary>
        public int priority;
        /// <summary>
        /// Có cho chơi tiếp game nếu mua xong không? 0: không, 1: có
        /// </summary>
        public int continueGame;
    }
}