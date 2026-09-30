/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-13
 */

using System;
using Falcon.Modules.Packs.Core.Runtime;
using TMPro;
using UnityEngine;

namespace Falcon.Modules.Packs.PacksBundle.Runtime
{
    /// <summary>
    /// Thành phần UI dùng để hiển thị một gói vật phẩm trong shop.
    /// Gán thông tin phần thưởng, giá và thời gian từ dữ liệu cấu hình.
    /// </summary>
    public class UIPackBundleElement : APackElement<WrapperPacksBundle>
    {
        [SerializeField] private TMP_Text textName, textTag;
        [SerializeField] private GameObject goKeepSpace;
        [SerializeField] private GameObject[] goKeepSpaces;
        [SerializeField] private GameObject groupBooster;
        [SerializeField] private GameObject groupLive;
        [SerializeField] private Sprite[] sGoldIcon;
        [SerializeField] private int[] goldMilestones = { 4000, 7000, 14000, 30000, 60000 };

        protected override void OnLocalize()
        {
            if (_config is not BundleElementPackConfig bundleConfig) return;
            textName.text = bundleConfig.name.Localize();
            textTag.text = bundleConfig.tag.Localize();
        }

        protected override void SetupUI()
        {
            base.SetupUI();
            if (_config is not BundleElementPackConfig bundleConfig) return;
            
            textName.text = bundleConfig.name.Localize();
            if (!string.IsNullOrEmpty(bundleConfig.tag))
            {
                textTag.transform.parent.gameObject.SetActive(true);
                textTag.text = bundleConfig.tag.Localize();
            }
            else
            {
                textTag.transform.parent.gameObject.SetActive(false);
            }
            
            if (_dictItem.TryGetValue("gold", out var itemGold))
            {
                itemGold.Icon.sprite = sGoldIcon[GetGoldIconIndex(itemGold.data.amount)];
                itemGold.Icon.SetNativeSize();
            }

            goKeepSpace.SetActive(false);
            foreach (var go in goKeepSpaces) go.SetActive(false);
            if (groupLive.GetComponentsInChildren<UIItemReward>().Length == 0)
            {
                groupLive.SetActive(false);
                goKeepSpace.SetActive(true);
                foreach (var go in goKeepSpaces) go.SetActive(true);
            }
            else
            {
                groupLive.SetActive(true);
            }

            if (groupBooster.GetComponentsInChildren<UIItemReward>().Length == 0)
            {
                groupBooster.SetActive(false);
                goKeepSpace.SetActive(true);
                foreach (var go in goKeepSpaces) go.SetActive(true);
            }
            else
            {
                groupBooster.SetActive(true);
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