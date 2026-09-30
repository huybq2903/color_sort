using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.Tutorial;

namespace Falcon.Shared.BaseBooster
{
    /// <summary>Hỏi xác nhận trước khi dùng 1 booster. Mỗi booster 1 class con kèm [Tutorial("confirm_use_{Type}")].</summary>
    public abstract class ATutorialConfirmUseBooster : ATutorial
    {
        public ABoosterLogic BoosterLogic { get; set; }

        public override void OnInitialize(ATutorialManager tutorialManager)
        {
            base.OnInitialize(tutorialManager);
            _steps.Add(new TutorialStepOpenConfirmBooster { TutorialConfirm = this });
            _steps.Add(new TutorialStepExecuteBooster { TutorialConfirm = this });
        }
    }

    public class TutorialStepOpenConfirmBooster : ATutorialStep
    {
        public override void StartStep()
        {
            UIWrapper.OpenPopup("UIPopupConfirmUseBooster", popup =>
           {
               var uiPopup = popup.GetComponent<UIPopupConfirmUseBooster>();
               uiPopup.ConfirmUseTut = TutorialConfirm;
               uiPopup.SetData();
           });
        }

        public override void StopStep()
        {
            UIWrapper.ClosePopup("UIPopupConfirmUseBooster");
        }

        public ATutorialConfirmUseBooster TutorialConfirm { get; set; }
    }

    public class TutorialStepExecuteBooster : ATutorialStep
    {
        public override void StartStep()
        {
            TutorialConfirm.BoosterLogic.Execute();
            TutorialConfirm.BoosterLogic.AfterExecute();
        }

        public override void StopStep() { }

        public ATutorialConfirmUseBooster TutorialConfirm { get; set; }
    }
}
