/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class UIHomeShortcuts : MonoBehaviour
    {
        public RectTransform layoutExpand;
        public RectTransform layoutLeft;
        public RectTransform layoutRight;

        [Header("Point End")]
        public RectTransform pointEndPlay;

        protected UIHomeShortcutsConfig _config;
        protected int _maxNumberActiveItem = -1;

        protected virtual void Awake()
        {
            //Cache
            _config = Resources.Load<UIHomeShortcutsConfig>("SO_UI_Menu_Home_ShortcutsConfig");
            
            //Spacing Item
            layoutLeft.GetComponent<VerticalLayoutGroup>().spacing = _config.spacingChildItem;
            layoutRight.GetComponent<VerticalLayoutGroup>().spacing = _config.spacingChildItem;
        }

        protected virtual void OnEnable()
        {
            Refresh();
        }

        protected virtual void LateUpdate()
        {
            UpdateUI_PositionLayout(layoutExpand.childCount > 0);
            UpdateUI_ScaleLayout();
        }

        [Button]
        protected virtual void Refresh()
        {
            _maxNumberActiveItem = -1;
            LateUpdate();
        }

        protected virtual void UpdateUI_PositionLayout(bool expand)
        {
            if (expand)
            {
                layoutLeft.anchoredPosition = new Vector2(-_config.originalAnchorPosXLayout, _config.expandAnchorPosYLayout);
                layoutRight.anchoredPosition = new Vector2(_config.originalAnchorPosXLayout, _config.expandAnchorPosYLayout);
            }
            else
            {
                layoutLeft.anchoredPosition = new Vector2(-_config.originalAnchorPosXLayout, _config.originalAnchorPosYLayout);
                layoutRight.anchoredPosition = new Vector2(_config.originalAnchorPosXLayout, _config.originalAnchorPosYLayout);
            }
            layoutExpand.anchoredPosition = new Vector2(layoutExpand.anchoredPosition.x, _config.anchorPosYExpand);
        }

        protected virtual void UpdateUI_ScaleLayout()
        {
            var numberLeft = GetNumberActive(layoutLeft);
            var numberRight = GetNumberActive(layoutRight);
            var min = Mathf.Max(numberLeft, numberRight);

            if (_maxNumberActiveItem != min)
            {
                //Cache
                _maxNumberActiveItem = min;

                if (_maxNumberActiveItem == 0) return;

                SetSizeDeltaHeightToPointEndPlay(layoutLeft);
                SetSizeDeltaHeightToPointEndPlay(layoutRight);

                SetScaleLayout(layoutLeft);
                SetScaleLayout(layoutRight);
            }
        }

        protected virtual int GetNumberActive(RectTransform layout)
        {
            int num = 0;      
            for (int i = 0; i < layout.childCount; i++)
            {
                if(layout.GetChild(i).gameObject.activeInHierarchy) num++;
            }
            return num;
        }

        protected virtual void SetSizeDeltaHeightToPointEndPlay(RectTransform layout)
        {
            //Default
            layout.transform.localScale = Vector3.one;

            //Caculate Local Point Layout -> Point End Play
            pointEndPlay.anchoredPosition = new Vector2(pointEndPlay.anchoredPosition.x, _config.anchorPosYTargetEnd);
            var localPoint = layout.InverseTransformPoint(pointEndPlay.transform.position);
            var newHeight = Mathf.Abs(localPoint.y);
            layout.sizeDelta = new Vector2(layout.sizeDelta.x, newHeight);
        }

        protected virtual void SetScaleLayout(RectTransform layout)
        {   
            var heightByItemActive = (_maxNumberActiveItem * _config.heightChildItem) + (_maxNumberActiveItem * _config.spacingChildItem);
            var heightByPointEndPlay = layout.sizeDelta.y;
            var scaleNew = (float) heightByPointEndPlay / heightByItemActive;      
            if (scaleNew > 1 || scaleNew < 0) scaleNew = 1f;
            layout.transform.localScale = Vector3.one * scaleNew;
        }
    }
}
