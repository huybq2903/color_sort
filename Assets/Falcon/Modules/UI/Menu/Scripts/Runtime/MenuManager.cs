/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using System.Collections.Generic;
using DanielLochner.Assets.SimpleScrollSnap;
using UnityEngine;
using UnityEngine.UI;
using Falcon.Helpers.EventBus;
using Falcon.Helpers.UI;

namespace Falcon.Modules.UI.Menu.Runtime
{
    [DefaultExecutionOrder(-999)]
    public class MenuManager : MonoBehaviour
    {
        public ScrollRect scrollRect;
        public SimpleScrollSnap simpleScrollSnap;
        public MenuNavigator navigator;
        public RectTransform addons;

        [HideInInspector]
        public List<RectTransform> tabFeatures = new();

        protected MenuFeaturesConfig _menuConfig;
        
        protected virtual void Awake()
        {
            //Cache
            _menuConfig = Resources.Load<MenuFeaturesConfig>("SO_UI_Menu_FeaturesConfig");
        }

        protected virtual void Start()
        {
            Init();
        }

        protected virtual void OnEnable()
        {
            GameRequest<GameObject>.Register(MenuConst.EVENT_MENU_GET_GAME_OBJECT, () => gameObject, this);
        }

        protected virtual void OnDisable()
        {
            GameRequest<GameObject>.Unregister(MenuConst.EVENT_MENU_GET_GAME_OBJECT);
        }

        protected virtual void Init()
        {
            //Force Update Canvas To Calculate Size Correctly
            Canvas.ForceUpdateCanvases();

            simpleScrollSnap.Start();

            //Remove Tab Not Use
            var numberTabDeactive = 5 - _menuConfig.maxNumberTab;
            for (int i = 0; i < numberTabDeactive; i++)
            {
                simpleScrollSnap.RemoveFromBack();
            }

            navigator.InitNavigator((scrollRect, simpleScrollSnap, _menuConfig.defaultTab));

            for (int i = 0; i < _menuConfig.maxNumberTab; i++)
            {
                LoadFeatures(GetTab(i), _menuConfig.GetFeature(i));
            }

            LoadFeatures(addons, _menuConfig.featureAddons);

            GameEvent.Emit(MenuConst.EVENT_MENU_LOAD_FEATURES_COMPLETE);
            Debug.Log("Menu Load Features Completed!");
        }

        // Nạp đồng bộ nên sibling index gán thẳng theo thứ tự config, khỏi đếm số feature đã xong
        private void LoadFeatures(RectTransform parent, List<string> features)
        {
            if (parent == null) return;

            for (int i = 0; i < features.Count; i++)
            {
                var comp = parent.gameObject.AddComponent<LoadPrefabFromAddressable>();
                comp.LoadSync(features[i]);
                if (comp.Prefab) comp.Prefab.transform.SetSiblingIndex(i);
            }
        }

        public virtual RectTransform GetTab(int index)
        {
            if (tabFeatures.Count == 0)
            {
                for (int i = 0; i < scrollRect.content.childCount; i++)
                {
                    var item = scrollRect.content.GetChild(i).GetComponent<RectTransform>();
                    tabFeatures.Add(item);
                }
            }

            if (index < 0 || index >= tabFeatures.Count) return null;
            return tabFeatures[index];
        }
    }
}
