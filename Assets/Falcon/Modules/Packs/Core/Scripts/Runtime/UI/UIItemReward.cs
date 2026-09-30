/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-24
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Packs.Core.Runtime
{
    public class UIItemReward : MonoBehaviour
    {
        public Image Icon
        {
            get
            {
                _icon ??= GetComponentInChildren<Image>(true);
                return _icon;
            }
        }
        
        public TMP_Text TextAmount
        {
            get
            {
                _textAmount ??= GetComponentInChildren<TMP_Text>(true);
                return _textAmount;
            }
        }

        public ABaseElementPackConfig.Reward data;

        private Image _icon;
        private TMP_Text _textAmount;

        public virtual void SetText()
        {
            TextAmount.text = FormatHelper.FormatQuantityReward((data.name, data.amount, data.data));
        }

        public virtual void SetIcon()
        {
            
        }
    }
}