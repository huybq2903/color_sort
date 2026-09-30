using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.UI.Runtime;
using UnityEngine;
using Falcon.Shared.Common;

namespace Falcon.Shared.EasyPopup
{
    public class UIPopupFullScreenHandler : MonoBehaviour
    {
        private void OnEnable()
        {
            UIWrapper.onPopupChanged += OnPopupChanged;
        }

        private void OnDisable()
        {
            UIWrapper.onPopupChanged -= OnPopupChanged;
        }

        private void OnPopupChanged()
        {
            var isFullScreen = !UIWrapper.IsFree_UIPopupFullScreen();
            GameEvent<float>.Emit(GameKeys.SET_ALPHA_HUD, isFullScreen ? 0f : 1f);
        }
    }
}