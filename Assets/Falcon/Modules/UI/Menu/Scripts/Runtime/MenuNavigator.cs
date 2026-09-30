/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using UnityEngine;
using DanielLochner.Assets.SimpleScrollSnap;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using I2.Loc;
using System;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class MenuNavigator : MonoBehaviour
    {
        public RectTransform viewport;
        public RectTransform rectScrollBarSelected;
        public Scrollbar scrollBarSelected;
        public RectTransform handleSelected;
        public HorizontalLayoutGroup layoutGroupPoint;
        public List<RectTransform> listPoint;

        [Title("Tabs")]
        public List<Button> listButtonTab;
        public List<MenuTabAnimatorBase> listTabAnimators;

        [HideInInspector]
        public ScrollRect scrollRect;

        [HideInInspector]
        public SimpleScrollSnap scrollSnap;

        [HideInInspector]
        public MenuNavigatorConfig uIHomeNavigatorConfig;

        [HideInInspector]
        public int activeCount;

        [HideInInspector]
        public int indexTabSelect;

        protected virtual void OnEnable()
        {
            LocalizationManager.OnLocalizeEvent += OnLocalizeEvent;
            GameEvent<(int index, bool isForceSnap)>.Register(MenuConst.EVENT_MENU_NAVIGATOR_GO_TO_TAB, OnGoToTab, this);
            GameEvent<Action<int>>.Register(MenuConst.EVENT_MENU_NAVIGATOR_GET_CURRENT_INDEX_TAB, GetCurrentIndexTab, this);
            GameEvent<Action<GameObject>>.Register(MenuConst.EVENT_MENU_NAVIGATOR_GET_GAME_OBJECT, GetThisGameObject, this);
        }

        protected virtual void OnDisable()
        {
            LocalizationManager.OnLocalizeEvent -= OnLocalizeEvent;
            GameEvent<(int index, bool isForceSnap)>.Unregister(MenuConst.EVENT_MENU_NAVIGATOR_GO_TO_TAB, OnGoToTab, this);
            GameEvent<Action<int>>.Unregister(MenuConst.EVENT_MENU_NAVIGATOR_GET_CURRENT_INDEX_TAB, GetCurrentIndexTab, this);
            GameEvent<Action<GameObject>>.Unregister(MenuConst.EVENT_MENU_NAVIGATOR_GET_GAME_OBJECT, GetThisGameObject, this);
        }

        protected virtual void OnLocalizeEvent()
        {
            UpdateTextAllTab();
        }

        protected virtual void OnGoToTab((int index, bool isForceSnap) data)
        {
            GoToTab(data.index, data.isForceSnap);
        }

        protected void GetCurrentIndexTab(Action<int> callback)
        {
            callback?.Invoke(scrollSnap.CenteredPanel);
        }

        protected void GetThisGameObject(Action<GameObject> callback)
        {
            callback?.Invoke(gameObject);
        }

        public virtual void InitNavigator((ScrollRect scrollRect, SimpleScrollSnap simpleScrollSnap, int defaultTab) data)
        {
            uIHomeNavigatorConfig = Resources.Load<MenuNavigatorConfig>("SO_UI_Menu_NavigatorConfig");

            //Cache
            scrollRect = data.scrollRect;
            scrollSnap = data.simpleScrollSnap;
            activeCount = uIHomeNavigatorConfig.maxNumberTab;

            void TrimList<T>(List<T> list)
            {
                if (list != null)
                {
                    for (int i = list.Count - 1; i >= activeCount; i--)
                    {
                        if (list[i] != null && list[i] is Component comp)
                        {
                            Destroy(comp.gameObject);
                        }
                        list.RemoveAt(i);
                    }
                }
            }

            if (activeCount < 5) TrimList(listPoint);
            TrimList(listButtonTab);
            TrimList(listTabAnimators);

            layoutGroupPoint.CalculateLayoutInputVertical();
            layoutGroupPoint.CalculateLayoutInputHorizontal();
            layoutGroupPoint.SetLayoutVertical();
            layoutGroupPoint.SetLayoutHorizontal();

            //Caculate Size Point by listPoint & layoutGroupPoint
            var totalWidth = layoutGroupPoint.GetComponent<RectTransform>().rect.width;
            var spacing = layoutGroupPoint.spacing;
            var itemCount = listPoint.Count;
            var sizePoint = (totalWidth - layoutGroupPoint.padding.horizontal - (spacing * (itemCount - 1))) / itemCount;

            //Offset Left & Right RectScrollBar
            var offset = activeCount < 5 ? 0 : sizePoint / 2;
            rectScrollBarSelected.offsetMin = new Vector2(offset, rectScrollBarSelected.offsetMin.y);
            rectScrollBarSelected.offsetMax = new Vector2(-offset, rectScrollBarSelected.offsetMax.y);

            //Size Viewport & Handler By Config
            viewport.sizeDelta = new Vector2(viewport.sizeDelta.x, uIHomeNavigatorConfig.heightViewport);
            rectScrollBarSelected.sizeDelta = new Vector2(rectScrollBarSelected.sizeDelta.x, uIHomeNavigatorConfig.heightScrollBarSelected);
            
            //Size Scroll Handler  
            handleSelected.sizeDelta = new Vector2(activeCount < 5 ? sizePoint : sizePoint * 2, handleSelected.sizeDelta.y);

            //Ratio Size ScrollBar
            scrollBarSelected.size = (float) 1 / activeCount;

            //Listing Change Tab
            scrollSnap.OnPanelSelected.RemoveAllListeners();
            scrollSnap.OnPanelSelected.AddListener((x) =>
            {
                if (gameObject.activeInHierarchy)
                {
                    MoveTabSelect(scrollSnap.CenteredPanel);
                    GameEvent<int>.Emit(MenuConst.EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED, scrollSnap.CenteredPanel);
                }
            });

            //Listing ScrollBar Tab Selected
            scrollRect.horizontalScrollbar = scrollBarSelected;
            scrollRect.onValueChanged?.Invoke(Vector2.zero);

            SetOnClickButtonTabs();
                    
            StartCoroutine(IEGoToPanelDefault());
            IEnumerator IEGoToPanelDefault()
            {
                scrollRect.content.anchoredPosition = new Vector2(-scrollSnap.RectTransform.rect.size.x * data.defaultTab, scrollRect.content.anchoredPosition.y);
                yield return new WaitForEndOfFrame();
                scrollSnap.GoToPanel(data.defaultTab);
                scrollSnap.StopSnapToPanel();
                DOTween.Complete(MenuConst.TWEEN_TAB_ANIMATOR_MENU_NAVIGATOR);
            }
        }

        protected virtual void SetOnClickButtonTabs()
        {
            for (int i = 0; i < listButtonTab.Count; i++)
            {
                var a = i;
                listButtonTab[a].onClick.AddListener(() =>
                {
                    if (gameObject.activeInHierarchy)
                    {
                        if (scrollRect.velocity != Vector2.zero) scrollRect.StopMovement();
                        scrollSnap.GoToPanel(a);
                    }
                });
            }
        }

        public virtual void GoToTab(int index, bool isForce = false)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(IEGoToPanel());
                IEnumerator IEGoToPanel()
                {
                    yield return new WaitForEndOfFrame();
                    scrollSnap.GoToPanel(index);
                    if (isForce) scrollSnap.StopSnapToPanel();
                }
            }
        }

        protected virtual void MoveTabSelect(int indexTab)
        {
            //Cache
            indexTabSelect = indexTab;

            //Animation
            for (int i = 0; i < listTabAnimators.Count; i++)
            {
                if (i == indexTab)
                {
                    listTabAnimators[i].SelectTab(this, i);
                }
                else
                {
                    listTabAnimators[i].DeselectTab(this, i);
                }
            }
            
            //Name Tab
            UpdateTextAllTab();
        }

        protected virtual void UpdateTextAllTab()
        {
            for (int i = 0; i < listTabAnimators.Count; i++)
            {
                var term = uIHomeNavigatorConfig.termI2NameTabs[i];
                if (string.IsNullOrEmpty(term)) continue;
                var strNameTab = LocalizationManager.GetTranslation(term);
                var text = strNameTab != null ? strNameTab : term;
                listTabAnimators[i].UpdateNameTab(this, text);
            }
        }
    }
}
