/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-29
 */

using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.Tutorial;

namespace Falcon.Shared.BaseBooster
{
    /// <summary>Chỉ dẫn dùng thử 1 booster vừa mở khoá. Mỗi booster 1 class con kèm [Tutorial("unlock_{Type}")].</summary>
    public abstract class ATutorialUnlockBooster : ATutorial
    {
        public ABoosterLogic BoosterLogic { get; set; }

        public override void OnInitialize(ATutorialManager tutorialManager)
        {
            base.OnInitialize(tutorialManager);
            _steps.Add(new ATutorialStepOpenPopupUnlock { TutorialUnlock = this });
        }
    }

    public class ATutorialStepOpenPopupUnlock : ATutorialStep
    {
        public override void StartStep()
        {
            UIWrapper.OpenPopup("UIPopupUnlockBooster", popup =>
            {
                var uiPopup = popup.GetComponent<UIPopupUnlockBooster>();
                uiPopup.TutorialUnlock = TutorialUnlock;
                uiPopup.SetData();
            });
        }

        public override void StopStep()
        {
            UIWrapper.ClosePopup("UIPopupUnlockBooster");
        }

        public ATutorialUnlockBooster TutorialUnlock { get; set; }
    }
}
