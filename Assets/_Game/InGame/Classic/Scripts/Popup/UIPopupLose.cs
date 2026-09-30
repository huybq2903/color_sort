// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-05-14

using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.UI;
using Falcon.Shared.Common;
using Falcon.Modules.Core.UI.Runtime;

namespace Falcon.InGame.UI
{
    public class UIPopupLose : MonoBehaviour
    {
        public Button btnExit;
        public Button btnRestart;

        private void Awake()
        {
            btnExit.onClick.AddListener(() =>
            {
                GameEvent.Emit(GameKeys.BACK_TO_HOME);
            });

            btnRestart.onClick.AddListener(() =>
            {
                if (GameRequest<bool>.Request(GameKeys.CAN_USE_LIVE))
                {
                    GameEvent.Emit(GameKeys.PLAY_LEVEL);
                }
                else
                {
                    GameEvent<string>.Emit(Const.EVENT_OPEN_POPUP_NAME, "UIPopupRefillLives");
                }
            });
        }
    }
}