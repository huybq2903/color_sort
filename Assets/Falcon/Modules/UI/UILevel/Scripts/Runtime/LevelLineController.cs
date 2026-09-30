/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-23
 */

using System;
using UnityEngine;

namespace Falcon.Modules.UI.Level.Runtime
{
    public class LevelLineController : MonoBehaviour
    {
        [SerializeField] private LevelLineView _view;

        private static LevelLineController _instance;
        private static Action _laterShowAction;

        private void Start()
        {
            _instance = this;
            _laterShowAction?.Invoke();
            _laterShowAction = null;
        }

        public static void Show(ILevelLine implementer)
        {
            if (_instance == null)
            {
                _laterShowAction = () => _instance.ShowLevelLine(implementer);
                return;
            }

            _instance.ShowLevelLine(implementer);
        }

        private void ShowLevelLine(ILevelLine implementer)
        {
            var numOfLevels = implementer.GetNumberOfLevels();
            var currentLevel = implementer.GetCurrentLevel();
            var winStreak = implementer.GetWinStreak();

            var model = new LevelLineModel(numOfLevels, currentLevel, winStreak, implementer.GetDifficultyAction,
                implementer.HasRewardsAction);
            var presenter =
                new LevelLinePresenter(_view, model, implementer.OnClickLevelNode, implementer.OnClickBtnPlay);
            presenter.Render();
        }
    }
}