/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using Falcon.Helpers.EventBus;
using Falcon.Modules.UI.Level.Runtime;
using UnityEngine;
using Falcon.Shared.Common;
using Falcon.Modules.Core.UI.Runtime;

namespace Falcon.OutGame.Core
{
    public class UIHomeLevelLine : MonoBehaviour, ILevelLine
    {
        public int winStreak;
        public int numOfLevels = 10;

        void Awake()
        {
            LevelLineController.Show(this);
        }

        public int GetNumberOfLevels()
        {
            return numOfLevels;
        }

        public int GetCurrentLevel()
        {
            return GameRequest<int>.Request(GameKeys.GET_LEVEL);
        }

        public int GetWinStreak()
        {
            return winStreak;
        }

        public int GetDifficultyAction(int level)
        {
            return GameRequest<int, int>.Request(GameKeys.GET_LEVEL_DIFFICULTY, level);
        }

        public bool HasRewardsAction(int level)
        {
            return false;
        }

        public void OnClickLevelNode(LevelNodeModel model)
        {
        }

        public void OnClickBtnPlay()
        {
            if (GameRequest<bool>.Request(GameKeys.CAN_USE_LIVE))
            {
                GameEvent.Emit(GameKeys.PLAY_LEVEL);
            }
            else
            {
                GameEvent<string>.Emit(Const.EVENT_OPEN_POPUP_NAME, "UIPopupRefillLives");
            }
        }
    }
}
