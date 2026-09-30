/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.Tutorial;

namespace Falcon.Shared.BaseObstacle
{
    /// <summary>Giới thiệu 1 obstacle lần đầu gặp. Mỗi obstacle 1 class con kèm [Tutorial("unlock_{Type}")].</summary>
    public abstract class ATutorialUnlockObstacle : ATutorial
    {
        public ABaseObstacleLogic ObstacleLogic { get; set; }

        public override void OnInitialize(ATutorialManager tutorialManager)
        {
            base.OnInitialize(tutorialManager);
            _steps.Add(new ATutorialStepOpenPopupUnlockObstacle { TutorialUnlock = this });
        }
    }

    public class ATutorialStepOpenPopupUnlockObstacle : ATutorialStep
    {
        public ATutorialUnlockObstacle TutorialUnlock { get; set; }

        public override void StartStep()
        {
            UIWrapper.OpenPopup("UIPopupUnlockObstacle", popup =>
            {
                var uiPopup = popup.GetComponent<UIPopupUnlockObstacle>();
                uiPopup.TutorialUnlock = TutorialUnlock;
                uiPopup.SetData();
            });
        }

        public override void StopStep()
        {
            UIWrapper.ClosePopup("UIPopupUnlockObstacle");
        }
    }
}
