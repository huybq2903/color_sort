/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-02
*/

using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.UI.Toast.Runtime
{
    public class UIToastConfig : ScriptableObject
    {
        [InfoBox("Thời gian tổng tự động đóng toast. Khi thêm mới 1 toast sẽ tự tính lại thời gian")]
        public float lifetime = 1.5f;

        [InfoBox("Khoảng cách mỗi toast xuất hiện")]
        public float spacingItem = 250f;

        [InfoBox("Số lượng tối đa toast trên màn hình")]
        public int maxItemInstance = 3;

        [Header("Tween")]
        [InfoBox("Tween cho mỗi toast item")]
        public float durationScale = 0.325f;
        public Ease easeScaleIn = Ease.OutBack;
        public Ease easeScaleOut = Ease.InBack;
        public float durationMoveUp = 0.25f;
        public Ease easeMoveUp = Ease.InOutSine;
    }
}
