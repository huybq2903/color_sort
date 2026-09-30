using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.EasyPopup
{
    public class UIPopupAutoFade : UIPopupBase
    {
        [SerializeField] protected Button btnBack;

        protected virtual void Awake()
        {
            if (btnBack) btnBack.onClick.AddListener(OnClickBack);
        }
    }
}