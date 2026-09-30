/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-1
*/

using TMPro;
using UnityEngine;

namespace Falcon.Modules.UI.Toast.Runtime
{
    public class UIToastItem : MonoBehaviour
    {
        public TextMeshProUGUI txtContent;

        public virtual void SetItemData(string content)
        {
            txtContent.SetText(content);
        }
    }
}
