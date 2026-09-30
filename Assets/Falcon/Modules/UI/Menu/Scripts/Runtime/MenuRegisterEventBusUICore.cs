using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.UI.Runtime;
using UnityEngine;

namespace Falcon.Modules.UI.Menu.Runtime
{
    public static class MenuRegisterEventBusUICore
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void Init()
        {
            GameEvent<Transform>.Register(Const.EVENT_OPEN_POPUP, popup =>
            {
                if (UIWrapper.Manager != null) UIWrapper.OpenPopup(popup, p => GameEvent<Transform>.Emit(Const.EVENT_ON_OPEN_POPUP, p));
            }, null);

            GameEvent<string>.Register(Const.EVENT_OPEN_POPUP_NAME, name =>
            {
                if (UIWrapper.Manager != null) UIWrapper.OpenPopup(name, p => GameEvent<Transform>.Emit(Const.EVENT_ON_OPEN_POPUP, p));
            }, null);

            GameEvent<Transform>.Register(Const.EVENT_CLOSE_POPUP, popup =>
            {
                if (UIWrapper.Manager != null) UIWrapper.ClosePopup(popup);
            }, null);

            GameEvent<string>.Register(Const.EVENT_CLOSE_POPUP_NAME, name =>
            {
                if (UIWrapper.Manager != null) UIWrapper.ClosePopup(name);
            }, null);
        }
    }
}
