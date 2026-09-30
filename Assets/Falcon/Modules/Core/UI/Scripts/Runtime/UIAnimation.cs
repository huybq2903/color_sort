/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-03
*/

using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Modules.Core.UI.Runtime
{
    /// <summary>
    /// Base class for UI animations. Contains basic show/hide logic with duration control.
    /// </summary>
    public class UIAnimation : MonoBehaviour
    {
        /// <summary>
        /// Duration of the animation in seconds.
        /// </summary>
        public float duration = 0.275f;

        public bool isOverrideDuration = false;

        [InfoBox("If enabled, new duration values for show and hide animations will be used instead of the default duration.")]
        [ShowIf("isOverrideDuration")]
        public float durationShow = 0.275f;

        [ShowIf("isOverrideDuration")]
        public float durationHide = 0.275f;
        
        public virtual void Init(UIBase parentUIBase)
        {

        }

        /// <summary>
        /// Called when the animation is shown. Override in derived classes to implement custom behavior.
        /// </summary>
        /// <param name="parentUIBase">Reference to the parent UIBase component.</param>
        public virtual void Show(UIBase parentUIBase)
        {
        }

        /// <summary>
        /// Called when the animation is hidden. Override in derived classes to implement custom behavior.
        /// </summary>
        /// <param name="parentUIBase">Reference to the parent UIBase component.</param>
        public virtual void Hide(UIBase parentUIBase)
        {
        }
    }
}
