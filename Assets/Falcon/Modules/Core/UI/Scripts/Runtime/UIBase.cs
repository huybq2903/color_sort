/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-03
*/

using System;
using System.Collections;
using UnityEngine;

namespace Falcon.Modules.Core.UI.Runtime
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIBase : MonoBehaviour
    {
        public Action onShow;
        public Action onShowLayoutCompleted;
        public Action onHide;

        [HideInInspector]
        public float hidingTime = 0f;

        [HideInInspector]
        public bool deactivateWhileInvisible = true;

        [HideInInspector]
        public bool visible;

        [HideInInspector]
        public CanvasGroup canvasGroup;

        [HideInInspector]
        public UIAnimation uiAnimation;

        protected bool _initialized;

        protected void InitializeElements()
        {
            if (_initialized) return;

            canvasGroup = GetComponent<CanvasGroup>();
            uiAnimation = GetComponent<UIAnimation>();

            //Set Hiding Time
            if (uiAnimation != null)
            {
                uiAnimation.Init(this);
                hidingTime = uiAnimation.isOverrideDuration ? uiAnimation.durationHide : uiAnimation.duration;
            }
            
            _initialized = true;
        }

        protected void ShowAnimation()
        {
            canvasGroup.interactable = true;
            if (uiAnimation != null) uiAnimation.Show(this);
        }

        protected void HideAnimation()
        {
            canvasGroup.interactable = false;
            if (uiAnimation != null) uiAnimation.Hide(this);
        }

        /// <summary>
        /// Change the visibilty of the object by playing the desired animation.
        /// </summary>
        /// <param name="visible">Should this element be visible?</param>
        public virtual void ChangeVisibility(bool visible)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Switch the visibility of the object by playing the desired animation.
        /// </summary>
        public virtual void SwitchVisibility()
        {
            ChangeVisibility(!visible);
        }

        /// <summary>
        /// Deactivate this element's Game Object.
        /// </summary>
        protected virtual void DeactivateMe()
        {
            gameObject.SetActive(false);
        }
    }
}
