using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.UI;
using Falcon.Shared.Common;

namespace Falcon.InGame.UI
{
    public class UIPopupQuitLevel : MonoBehaviour
    {
        public Button btnQuit;
        // public UIWarningLosePanel uiWarningLose;

        private void Awake()
        {
            btnQuit.onClick.AddListener(OnLeave);
            // uiWarningLose.Preload();
        }

        private void OnLeave()
        {
            // if (!uiWarningLose.TryOpenPanel())
            {
                GameEvent.Emit(GameKeys.BACK_TO_HOME);
            }
        }
    }
}