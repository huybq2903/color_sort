/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-27
 */

using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Packs.PacksRemoveAds.Runtime
{
    public class UIPopup_RemoveAds : MonoBehaviour
    {
        private const string EVENT_CLOSE_POPUP = "falcon.modules.core.ui_close_popup";
        
        [SerializeField] private Button bClose;
        [SerializeField] private string idPack;
        private void Awake()
        {
            bClose.onClick.RemoveAllListeners();
            bClose.onClick.AddListener(() =>
            {
                GameEvent<Transform>.Emit(EVENT_CLOSE_POPUP, transform);
            });
        }
        
        private void Start()
        {
            SendMessage("Setup", idPack, SendMessageOptions.DontRequireReceiver);
        }
    }
}