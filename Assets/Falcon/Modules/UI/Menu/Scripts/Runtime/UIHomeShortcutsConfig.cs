/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-30
*/

using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class UIHomeShortcutsConfig : ScriptableObject
    {
        [InfoBox("Có thể mở prefab để xem cấu trúc các điểm neo phần này", InfoMessageType.Warning)]
        [Header("Anchor Position - Layouts")]
        [InfoBox("Vị trí Y của phần mở rộng")]
        public float anchorPosYExpand = -220f;
        
        [InfoBox("Vị trí (X,Y) cột ban đầu")]
        public float originalAnchorPosXLayout = 410f;
        public float originalAnchorPosYLayout = -220f;

        [InfoBox("Vị trí Y cột nếu kích hoạt phần mở rộng")]
        public float expandAnchorPosYLayout = -440f;

        [InfoBox("Vị trí của điểm giới hạn cột. Để tự động thu nhỏ nếu số lượng shortcut vượt quá giới hạn")]
        public float anchorPosYTargetEnd = 450f;

        [Header("Size Delta")]
        [InfoBox("Chiều cao của một shortcut")]
        public float heightChildItem = 200f;

        [InfoBox("Khoảng cách giữa các shortcut")]
        public float spacingChildItem = 25f;
    }
}
