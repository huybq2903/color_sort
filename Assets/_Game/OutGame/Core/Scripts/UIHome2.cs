// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-04-22

using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Modules.UI.Menu.Runtime;
using Falcon.Shared.Common;
using Falcon.Shared.RewardFlow;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.OutGame.Core
{
    public class UIHome2 : MonoBehaviour
    {
        public Button btnSettings;
        public UIResource uiGold;

        protected virtual void OnDisable() => HomeFlowRunner.Stop();

        protected virtual void Start()
        {
            HomeFlowRunner.Run().Forget();
            GameEvent.Emit(MenuConst.EVENT_UI_HOME_LOAD_COMPLETE);
            var wrapperGold = Center.Get<WrapperGoldHome>();
            if (wrapperGold != null)
            {
                uiGold.ReplaceValue(wrapperGold.PrevGold).DoImmediately();
            }
            else
            {
                uiGold.ReplaceValue(ResourceCollector.Instance.GetResourceValueIntInCollector("gold")).DoImmediately();
            }
            btnSettings.onClick.RemoveAllListeners();
            btnSettings.onClick.AddListener(() =>
            {
                UIWrapper.OpenPopup("UIPopup_Settings");
            });
        }
    }
}