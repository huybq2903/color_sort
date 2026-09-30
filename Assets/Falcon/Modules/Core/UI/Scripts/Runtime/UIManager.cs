/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-03
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Falcon.Helpers.Singleton;

namespace Falcon.Modules.Core.UI.Runtime
{
    [DefaultExecutionOrder(-999)]
    public class UIManager : Singleton<UIManager>
    {
        public Action onUIPopupBeforeOpen;
        public Action onUIPopupBeforeClose;
        public Action onUIPopupChanged;

        [Header("Menus")]
        public List<UIMenu> menus = new List<UIMenu>();
        [HideInInspector] public UIMenu currentActiveMenu;

        [Header("Popups")]
        public List<UIPopup> popups = new List<UIPopup>();
        [HideInInspector] public UIPopup currentActivePopup;

        [Header("Store - UIPopup")]
        public RectTransform rootStorePopup;

        [ReadOnly] public List<UIPopup> stackPopups = new List<UIPopup>();

        protected override void Awake()
        {
            base.Awake();
            InitializeDefaultMenu();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            menus = null;
            popups = null;
        }

        private void InitializeDefaultMenu()
        {
            if (currentActiveMenu != null || menus.Count == 0) return;

            foreach (var menu in menus)
            {
                if (menu == null) continue;
                currentActiveMenu = menu;
                menu.gameObject.SetActive(true);
                menu.ChangeVisibility(true);
                break;
            }
        }

        #region Menu Functions

        public void OpenMenu_Stack(string menuName)
        {
            var currentMenu = currentActiveMenu;
            if (currentMenu != null && currentMenu.name == menuName) return;

            OpenMenu(menuName);
            if (currentActiveMenu != null)
                currentActiveMenu.previousMenu = currentMenu;
        }

        public void OpenMenu(string menuName)
        {
            var lastMenu = currentActiveMenu;
            foreach (var menu in menus)
            {
                if (menu == null) continue;

                if (menu.name == menuName)
                {
                    menu.gameObject.SetActive(true);
                    menu.ChangeVisibility(true);
                    currentActiveMenu = menu;
                    break;
                }
            }

            if (lastMenu != null && lastMenu != currentActiveMenu)
                lastMenu.ChangeVisibility(false);
        }

        public void OpenMenu(UIMenu menu)
        {
            var lastMenu = currentActiveMenu;

            if (menu != null && menus.Contains(menu))
            {
                menu.gameObject.SetActive(true);
                menu.ChangeVisibility(true);
                currentActiveMenu = menu;
            }

            if (lastMenu != null && lastMenu != currentActiveMenu)
                lastMenu.ChangeVisibility(false);
        }

        public void NextMenu()
        {
            if (currentActiveMenu?.nextMenu != null)
                OpenMenu(currentActiveMenu.nextMenu);
        }

        #endregion

        #region Popup Functions

        private HashSet<string> _popupFromAddressables = new HashSet<string>();

        // Chống spam: lưu các popup đang được load
        private HashSet<string> _popupLoading = new HashSet<string>();

        public void OpenPopup(string popupName, Action<Transform> callback = null)
        {
            //Load UIPopup - Default
            foreach (var popup in popups)
            {
                if (popup != null && popup.name == popupName)
                {
                    OpenPopup(popup, callback);
                    return;
                }
            }

            // Chống spam: nếu đang load popup này thì bỏ qua
            if (_popupLoading.Contains(popupName))
            {
                Debug.LogWarning($"Popup '{popupName}' is already loading.");
                return;
            }

            _popupLoading.Add(popupName);
            StartCoroutine(IELoad());
            IEnumerator IELoad()
            {
                if (_popupFromAddressables.Contains(popupName))
                {
                    yield return IELoadPopupFromAddressable(popupName, callback);
                    _popupLoading.Remove(popupName);
                    yield break;
                }

                var checkHandle = Addressables.LoadResourceLocationsAsync(popupName);
                yield return checkHandle;

                if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result.Count > 0)
                {
                    _popupFromAddressables.Add(popupName);
                    yield return IELoadPopupFromAddressable(popupName, callback);
                }
                else
                {
                    Debug.LogWarning($"Popup {popupName} not found in Addressables. Trying load popup scene");
                    yield return StartCoroutine(IELoadPopupFromScene(popupName, callback));
                }
                _popupLoading.Remove(popupName);
            }
        }
        
        private IEnumerator IELoadPopupFromAddressable(string popupName, Action<Transform> callback)
        {
            var handlerPopup = Addressables.LoadAssetAsync<GameObject>(popupName);
            yield return handlerPopup;

            if (handlerPopup.Status == AsyncOperationStatus.Succeeded && handlerPopup.Result != null)
            {
                var instance = Instantiate(handlerPopup.Result);

                if (instance.TryGetComponent<UIPopupAddressableHandler>(out var popupAddressableHandler))
                    popupAddressableHandler.Init(handlerPopup);
                else
                    instance.AddComponent<UIPopupAddressableHandler>().Init(handlerPopup);

                // Change Name
                instance.name = popupName;

                yield return new WaitForEndOfFrame();
                OpenPopup(instance.transform, callback);
            }
            else
            {
                Debug.LogWarning($"Addressables load failed for {popupName}: {handlerPopup.OperationException?.Message}");
            }
        }

        IEnumerator IELoadPopupFromScene(string popupName, Action<Transform> callback)
        {
            var asyncLoad = SceneManager.LoadSceneAsync(popupName, LoadSceneMode.Additive);
            if (asyncLoad == null) yield break;

            while (!asyncLoad.isDone) yield return new WaitForEndOfFrame();
            var scenePopup = SceneManager.GetSceneByName(popupName);
            var rootObjects = scenePopup.GetRootGameObjects();
            var instance = rootObjects[0];
            
            // Change Name
            instance.name = popupName;

            yield return new WaitForEndOfFrame();
            OpenPopup(instance.transform, callback);

            // Unload scene after load popup
            SceneManager.UnloadSceneAsync(popupName);
        }

        public void OpenPopup(Transform popup, Action<Transform> callback = null)
        {
            if (!popup.TryGetComponent<UIPopup>(out var uiPopup)) uiPopup = popup.gameObject.AddComponent<UIPopup>();

            //Add UIPopup To Default
            popups.Add(uiPopup);
            OpenPopup(uiPopup, p => callback?.Invoke(p.transform));
        }

        public void OpenPopup(UIPopup popup, Action<Transform> callback = null)
        {
            onUIPopupBeforeOpen?.Invoke();
            
            // Cache
            currentActivePopup = popup;

            RegisterPopupStack(popup);
            StateActivatePopupStack(true);

            popup.ChangeVisibility(true);

            // Broadcast Simulation Awake
            var hasStarted = !popup.HasStarted;
            if (hasStarted)
            {
                popup.gameObject.BroadcastMessage("OnUIPopupAwake", SendMessageOptions.DontRequireReceiver);
            }

            // Invoke callback after popup is opened
            callback?.Invoke(popup.transform);

            // Broadcast Simulation Enable
            popup.gameObject.BroadcastMessage("OnUIPopupEnable", SendMessageOptions.DontRequireReceiver);

            // Broadcast Simulation Start
            if (hasStarted)
            {
                popup.MarkStarted();
                popup.gameObject.BroadcastMessage("OnUIPopupStart", SendMessageOptions.DontRequireReceiver);
            }

            onUIPopupChanged?.Invoke();
        }

        // public void ClosePopup()
        // {
        //     if (currentActivePopup != null)
        //     {
        //         currentActivePopup.ChangeVisibility(false);
        //         stackPopups.Remove(currentActivePopup);
        //     }

        //     currentActivePopup = GetTopMostPopupStack();
        //     StateActivatePopupStack(false);
        // }

        public void ClosePopup(string popupName)
        {
            foreach (var popup in popups)
            {
                if (popup.name == popupName)
                {
                    ClosePopup(popup);
                    return;
                }
            }

            Debug.LogWarning($"Popup '{popupName}' not found in list.");
        }

        public void ClosePopup(UIPopup popup)
        {
            onUIPopupBeforeClose?.Invoke();

            popup.ChangeVisibility(false);
            stackPopups.Remove(popup);
  
            currentActivePopup = GetTopMostPopupStack();
            StateActivatePopupStack(false);

            onUIPopupChanged?.Invoke();
        }

        public void CloseAllPopup()
        {
            for (int i = stackPopups.Count - 1; i >= 0; i--)
            {
                ClosePopup(stackPopups[i]);
            }
        }

        public void ClearAllStack()
        {
            stackPopups.Clear();
            onUIPopupChanged?.Invoke();
        }

        private void RegisterPopupStack(UIPopup popup)
        {
            stackPopups.Remove(popup);
            stackPopups.Add(popup);
        }

        public UIPopup GetTopMostPopupStack()
        {
            for (int i = stackPopups.Count - 1; i >= 0; i--)
            {
                if (stackPopups[i] != null && stackPopups[i].visible)
                    return stackPopups[i];
            }

            return null;
        }

        private void StateActivatePopupStack(bool isOpenPopup)
        {
            if (currentActivePopup == null) return;

            // If popup in root, force move to root store
            if (currentActivePopup.transform.parent != rootStorePopup)
            {
                currentActivePopup.transform.SetParent(rootStorePopup);

                // Render full stretch rect transform
                var rect = currentActivePopup.GetComponent<RectTransform>();
                rect.localScale = Vector3.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.anchoredPosition3D = Vector3.zero;
            }

            // If open popup, always on top of hierarchy
            if (isOpenPopup)
                currentActivePopup.transform.SetAsLastSibling();
        }

        public List<UIPopup> GetAllPopupStack() => stackPopups;

        public int GetNumPopupStack() => stackPopups.Count;

        public bool IsFreeUIPopupFullScreen()
        {
            foreach (var item in stackPopups)
                if (item.isFullScreen) return false;
            return true;
        }

        public bool IsFreeUIPopupHalfScreen()
        {
            foreach (var item in stackPopups)
                if (item.isHalfScreen) return false;
            return true;
        }

        #endregion

        #region Utility

        public void OpenUI(string nameUI)
        {
            foreach (var menu in menus)
            {
                if (menu.name == nameUI)
                {
                    OpenMenu(nameUI);
                    return;
                }
            }

            foreach (var popup in popups)
            {
                if (popup.name == nameUI)
                {
                    OpenPopup(nameUI);
                    return;
                }
            }

            Debug.LogError($"Can't find UI named: {nameUI}");
        }

        public void Test_OpenPopup(UIPopup popupName)
        {
            OpenPopup(popupName);
        }
        #endregion
    }
}