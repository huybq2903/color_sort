/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
 */

using Falcon.Modules.Packs.Core.Runtime;
using UnityEngine;

namespace Falcon.Modules.Packs.PacksGold.Runtime
{
    /// <summary>
    /// Thành phần UI dùng để hiển thị một gói vật phẩm trong shop.
    /// Gán thông tin phần thưởng, giá và thời gian từ dữ liệu cấu hình.
    /// </summary>
    public class UIPackGoldElement : APackElement<WrapperPacksGold>
    {
        [SerializeField] private Sprite[] sGoldIcon;
        [SerializeField] private int[] goldMilestones = { 3000, 15000 };
        protected override void SetupUI()
        {
            base.SetupUI();
            foreach (var uiItem in _uiRewards)
            {
                uiItem.Icon.sprite = sGoldIcon[GetGoldIconIndex(uiItem.data.amount)];
                uiItem.Icon.SetNativeSize();
            }
        }

        private int GetGoldIconIndex(int amountGold)
        {
            var index = 0;
            while (index < goldMilestones.Length && amountGold > goldMilestones[index])
            {
                index++;
            }
            return index;
        }
    }
}