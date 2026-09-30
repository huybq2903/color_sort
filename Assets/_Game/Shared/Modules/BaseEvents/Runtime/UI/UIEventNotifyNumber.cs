using TMPro;
using UnityEngine;

namespace Falcon.Shared.BaseEvents
{
    public class UIEventNotifyNumber : UIEventNotify
    {
        [SerializeField] private TMP_Text txtNumNotify;
        
        protected override void OnChangeData(object data)
        {
            base.OnChangeData(data);
            txtNumNotify.text = Notify.ToString();
        }
    }
}