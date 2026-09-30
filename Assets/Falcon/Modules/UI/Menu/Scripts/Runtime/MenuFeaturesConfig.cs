/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
*/

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class MenuFeaturesConfig : ScriptableObject
    {
        [Header("Tabs")]
        [InfoBox("Số lượng tab tối đa và tab mặc định")]
        public int maxNumberTab = 3;
        public int defaultTab = 1;

        [Header("Features")]
        [InfoBox("DS tên tính năng tải vào các tab. Thứ tự từ trên xuống")]
        [InfoBox("Một tab sẽ tương ứng một list. Tối đa 5 tab thì có 5 list. Không điền nếu không sử dụng tab", InfoMessageType.Warning)]
        public List<string> featuresTab_1 = new List<string>();
        public List<string> featuresTab_2 = new List<string>();
        public List<string> featuresTab_3 = new List<string>();
        public List<string> featuresTab_4 = new List<string>();
        public List<string> featuresTab_5 = new List<string>();

        public List<string> GetFeature(int index)
        {
            switch (index)
            {
                case 0: return featuresTab_1;
                case 1: return featuresTab_2;
                case 2: return featuresTab_3;
                case 3: return featuresTab_4;
                case 4: return featuresTab_5;
            }
            return featuresTab_5;
        }

        [Header("Addons")]
        [InfoBox("DS tên tính năng bổ sung. Thứ tự từ trên xuống. Tất cả addons nằm ở layer trên cùng")]
        public List<string> featureAddons;
    }
}
