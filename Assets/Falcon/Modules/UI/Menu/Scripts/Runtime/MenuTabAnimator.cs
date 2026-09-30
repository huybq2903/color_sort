/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-16
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class MenuTabAnimator : MenuTabAnimatorBase
    {
        public RectTransform rectTransform;
        public Image icon;
        public TextMeshProUGUI text;

        public override void SelectTab(MenuNavigator manager, int index)
        {
            var config = manager.uIHomeNavigatorConfig;

            if (manager.activeCount >= 5)
            {
                var points = manager.listPoint;
                if (index < points.Count - 1)
                {
                    var positionX = (points[index].position.x + points[index + 1].position.x) / 2;
                    rectTransform.DOMoveX(positionX, config.durationMoveIcon).SetEase(config.easeMoveIcon).SetId(MenuConst.TWEEN_TAB_ANIMATOR_MENU_NAVIGATOR);
                }
            }

            icon.rectTransform.DOAnchorPos3DY(config.anchorPosYIconActive, config.durationMoveIcon).SetEase(config.easeMoveIcon).SetId(MenuConst.TWEEN_TAB_ANIMATOR_MENU_NAVIGATOR);
            icon.rectTransform.DOScale(config.scaleIconActive, config.durationScaleIconActive).SetEase(config.easeScaleIconActive).SetId(MenuConst.TWEEN_TAB_ANIMATOR_MENU_NAVIGATOR);
            icon.sprite = config.sprIconActives[index];
            icon.SetNativeSize();

            if (text.gameObject.activeInHierarchy == false)
            {
                text.gameObject.SetActive(true);
                text.transform.DOScale(config.scaleText, config.durationScaleText).From(0).SetEase(config.easeScaleText).SetId(MenuConst.TWEEN_TAB_ANIMATOR_MENU_NAVIGATOR);
            }
        }

        public override void DeselectTab(MenuNavigator manager, int index)
        {
            var config = manager.uIHomeNavigatorConfig;   

            if (manager.activeCount >= 5)
            {
                var points = manager.listPoint;
                var targetIndex = index;

                if (index > manager.indexTabSelect) targetIndex = index + 1;
                if (targetIndex >= 0 && targetIndex < points.Count)
                {
                    var positionX = points[targetIndex].position.x;
                    rectTransform.DOMoveX(positionX, config.durationMoveIcon).SetEase(config.easeMoveIcon).SetId(MenuConst.TWEEN_TAB_ANIMATOR_MENU_NAVIGATOR);
                }
            }

            icon.rectTransform.DOAnchorPos3DY(config.anchorPosYIconDeactive, config.durationMoveIcon).SetEase(config.easeMoveIcon).SetId(MenuConst.TWEEN_TAB_ANIMATOR_MENU_NAVIGATOR);
            icon.rectTransform.DOScale(config.scaleIconDeactive, config.durationScaleIconDeactive).SetEase(config.easeScaleIconDeactive).SetId(MenuConst.TWEEN_TAB_ANIMATOR_MENU_NAVIGATOR);
            icon.sprite = config.sprIconDeactives[index];
            icon.SetNativeSize();

            text.gameObject.SetActive(false);
            text.transform.DOKill();
        }

        public override void UpdateNameTab(MenuNavigator manager, string newText)
        {
            var config = manager.uIHomeNavigatorConfig;
            text.SetText(newText);
            text.rectTransform.anchoredPosition = new Vector2(text.rectTransform.anchoredPosition.x, config.anchorPosYText);
        }
    }
}
