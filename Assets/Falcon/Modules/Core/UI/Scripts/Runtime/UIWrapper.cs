/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-25
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Falcon.Modules.Core.UI.Runtime
{
    /// <summary>
    /// Provides a static wrapper to simplify interaction with the UIManager for popup handling.
    /// </summary>
    public static class UIWrapper
    {
        /// <summary>
        /// Event triggered when the popup stack changes.
        /// Subscribes/unsubscribes to UIManager's onUIPopupChanged event.
        /// </summary>
        public static event Action onPopupChanged
        {
            add { if (UIManager.HasInstance) UIManager.Instance.onUIPopupChanged += value; }
            remove { if (UIManager.HasInstance) UIManager.Instance.onUIPopupChanged -= value; }
        }

        /// <summary>
        /// Event triggered before a popup opens.
        /// Subscribes/unsubscribes to UIManager's onUIPopupBeforeOpen event.
        /// </summary>
        public static event Action onPopupBeforeOpen
        {
            add { if (UIManager.HasInstance) UIManager.Instance.onUIPopupBeforeOpen += value; }
            remove { if (UIManager.HasInstance) UIManager.Instance.onUIPopupBeforeOpen -= value; }
        }

        /// <summary>
        /// Event triggered before a popup closes.
        /// Subscribes/unsubscribes to UIManager's onUIPopupBeforeClose event.
        /// </summary>
        public static event Action onPopupBeforeClose
        {
            add { if (UIManager.HasInstance) UIManager.Instance.onUIPopupBeforeClose += value; }
            remove { if (UIManager.HasInstance) UIManager.Instance.onUIPopupBeforeClose -= value; }
        }

        /// <summary>
        /// Gets the singleton instance of the UIManager. For detailed UI management, use UIManager.Instance directly.
        /// Becareful when using this in Awake() methods, as the UIManager instance may not be initialized yet.
        /// </summary>
        public static UIManager Manager => UIManager.Instance;

        /// <summary>
        /// Opens a popup by its name.
        /// </summary>
        /// <param name="name">Name of the popup to open.</param>
        /// <param name="callback">Optional callback invoked with the popup's transform after it opens.</param>
        public static void OpenPopup(string name, Action<Transform> callback = null)
        {
            if (!UIManager.HasInstance) return;
            UIManager.Instance.OpenPopup(name, callback);
        }

        /// <summary>
        /// Opens a popup using a Transform reference.
        /// </summary>
        /// <param name="popup">Transform of the popup object to open.</param>
        /// <param name="callback">Optional callback invoked with the popup's transform after it opens.</param>
        public static void OpenPopup(Transform popup, Action<Transform> callback = null)
        {
            if (!UIManager.HasInstance) return;
            UIManager.Instance.OpenPopup(popup, callback);
        }

        /// <summary>
        /// Closes a popup by its name.
        /// </summary>
        /// <param name="name">Name of the popup to close.</param>
        public static void ClosePopup(string name)
        {
            if (!UIManager.HasInstance) return;
            UIManager.Instance.ClosePopup(name);
        }

        /// <summary>
        /// Closes a popup using its Transform.
        /// </summary>
        /// <param name="popup">Transform of the popup to close.</param>
        public static void ClosePopup(Transform popup)
        {
            if (!UIManager.HasInstance) return;
            UIManager.Instance.ClosePopup(popup.name);
        }

        /// <summary>
        /// Closes all currently open popups.
        /// </summary>
        public static void CloseAllPopup()
        {
            if (!UIManager.HasInstance) return;
            UIManager.Instance.CloseAllPopup();
        }

        /// <summary>
        /// Gets the current list of popups on the stack.
        /// </summary>
        /// <returns>A list of UIPopup objects currently on the popup stack.</returns>
        public static List<Transform> GetAllPopupStack()
        {
            if (!UIManager.HasInstance) return new List<Transform>();
            return UIManager.Instance.GetAllPopupStack().Select(x => x.transform).ToList();
        }

        /// <summary>
        /// Checks if a popup with the specified name is currently active in the hierarchy.
        /// </summary>
        /// <param name="namePopup">Name of the popup to check.</param>
        public static bool IsPopupStackActiveInHierarchy(string namePopup)
        {
            if (!UIManager.HasInstance) return false;
            var popup = UIManager.Instance.GetAllPopupStack().Find(x => x.name == namePopup);
            if (popup != null && popup.gameObject.activeInHierarchy)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the number of popups currently in the stack.
        /// </summary>
        /// <returns>Integer representing the number of active popups.</returns>
        public static int GetNumberPopupStack()
        {
            if (!UIManager.HasInstance) return 0;
            return UIManager.Instance.GetNumPopupStack();
        }

        /// <summary>
        /// Checks if the full-screen popup slot is currently free.
        /// </summary>
        /// <returns>True if no full-screen popup is active; otherwise, false.</returns>
        public static bool IsFree_UIPopupFullScreen()
        {
            if (!UIManager.HasInstance) return true;
            return UIManager.Instance.IsFreeUIPopupFullScreen();
        }

        /// <summary>
        /// Checks if the half-screen popup slot is currently free.
        /// </summary>
        /// <returns>True if no half-screen popup is active; otherwise, false.</returns>
        public static bool IsFree_UIPopupHalfScreen()
        {
            if (!UIManager.HasInstance) return true;
            return UIManager.Instance.IsFreeUIPopupHalfScreen();
        }
    }
}
