/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-06
 */

using Falcon.Helpers.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Falcon.Shared.Common;

namespace Falcon.Shared.RewardFlow
{
    public class UIRewardItem : MonoBehaviour
    {
        public SpriteAtlas atlas;
        public TMP_Text txtValue;
        public Image icon;
        public CanvasGroup canvasGroup;

        public void SetItemData(string id, int value)
        {
            icon.sprite = atlas.GetSprite(id);
            txtValue.text = GameRequest<(string, int), string>.Request(GameKeys.FORMAT_QUANTITY_REWARD, (id, value));
        }
    }
}