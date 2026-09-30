/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-2
*/

using System;
using System.Collections;
using Falcon.Modules.Core.UI.Runtime;
using UnityEngine;

namespace Falcon.Modules.UI.AdBreak.Runtime
{
    public class UIPopupAdBreak : MonoBehaviour
    {
        public void UpdateUI((float duration, Action OnClose) data)
        {
            StartCoroutine(IEHoldActive());
            IEnumerator IEHoldActive()
            {
                yield return new WaitForSeconds(data.duration);
                UIWrapper.ClosePopup(transform);
                data.OnClose?.Invoke();
            }
        }
    }
}
