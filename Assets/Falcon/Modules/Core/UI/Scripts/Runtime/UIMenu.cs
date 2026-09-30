/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-03
*/

using System.Collections;
using UnityEngine;

namespace Falcon.Modules.Core.UI.Runtime
{
    public class UIMenu : UIBase
    {
        [Header("The Watcher")]
        public UIMenu previousMenu;
        public UIMenu nextMenu;

        public override void ChangeVisibility(bool visible)
        {
            if (!_initialized)
                InitializeElements();

            base.visible = visible;

            if (visible)
            {
                ShowAnimation();
                onShow?.Invoke();

                StartCoroutine(IEOnShowLayout());
                IEnumerator IEOnShowLayout()
                {
                    yield return new WaitForEndOfFrame();
                    onShowLayoutCompleted?.Invoke();
                }
            }
            else if (!visible)
            {
                HideAnimation();
                onHide?.Invoke();
            }

            if (deactivateWhileInvisible)
            {
                if (!visible)
                    Invoke(nameof(DeactivateMe), hidingTime);
                else
                    CancelInvoke(nameof(DeactivateMe));
            }
        }
    }
}
