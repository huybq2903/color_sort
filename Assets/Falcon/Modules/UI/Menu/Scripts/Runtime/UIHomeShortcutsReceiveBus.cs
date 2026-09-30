/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

using Falcon.Helpers.EventBus;
using UnityEngine;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class UIHomeShortcutsReceiveBus : MonoBehaviour
    {
        public RectTransform layoutExpand;
        public RectTransform layoutLeft;
        public RectTransform layoutRight;
        public bool useCheckDuplicate = true;

        protected virtual void OnEnable()
        {
            GameEvent<Transform>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_LEFT, OnAddShortcutLeft, this);
            GameEvent<Transform>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_RIGHT, OnAddShortcutRight, this);
            GameEvent<Transform>.Register(MenuConst.EVENT_UI_HOME_ADD_SHORTCUT_EXPAND, OnAddShortcutExpand, this);
            GameEvent<Transform>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_REMOVE, OnRemoveShortcut, this);
        }

        protected virtual void OnDisable()
        {
            GameEvent<Transform>.Unregister(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_LEFT, OnAddShortcutLeft, this);
            GameEvent<Transform>.Unregister(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_RIGHT, OnAddShortcutRight, this);
            GameEvent<Transform>.Unregister(MenuConst.EVENT_UI_HOME_ADD_SHORTCUT_EXPAND, OnAddShortcutExpand, this);
            GameEvent<Transform>.Unregister(MenuConst.EVENT_UI_HOME_SHORTCUT_REMOVE, OnRemoveShortcut, this);
        }

        protected void OnAddShortcutLeft(Transform button)
        {
            if (button == null) return;
            button.transform.SetParent(layoutLeft);
            button.gameObject.SetActive(true);
            DestroyDuplicate(layoutLeft, button);
        }

        protected void OnAddShortcutRight(Transform button)
        {
            if (button == null) return;
            button.transform.SetParent(layoutRight);
            button.gameObject.SetActive(true);
            DestroyDuplicate(layoutRight, button);
        }

        protected void OnAddShortcutExpand(Transform button)
        {
            if (button == null) return;
            button.transform.SetParent(layoutExpand);
            button.gameObject.SetActive(true);
            DestroyDuplicate(layoutExpand, button);
        }
        
        private void DestroyDuplicate(RectTransform layout, Transform button)
        {
            // If not check duplicate, just return
            if (!useCheckDuplicate) return;

            string buttonName = string.Empty;
            if (button != null) buttonName = button.name;

            try
            {
                int count = 0;
                for (int i = 0; i < layout.childCount; i++)
                {
                    if (layout.GetChild(i).name == buttonName)
                        count++;
                }

                if (count > 1)
                {
                    for (int i = layout.childCount - 1; i >= 0; i--)
                    {
                        var child = layout.GetChild(i);
                        if (child.name == buttonName)
                        {
                            Destroy(child.gameObject);
                        }
                    }

                    if (button != null) Destroy(button.gameObject);
                }
            }
            catch
            {
                // If something went wrong, just destroy all
                for (int i = layout.childCount - 1; i >= 0; i--)
                {
                    var child = layout.GetChild(i);
                    if (child.name == buttonName)
                    {
                        Destroy(child.gameObject);
                    }
                }

                if (button != null) Destroy(button.gameObject);
            }
        }

        protected void OnRemoveShortcut(Transform button)
        {
            button.gameObject.SetActive(false);
        }
    }
}
