/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
*/

using System;

namespace Falcon.Modules.UI.Level.Runtime
{
    public interface ILevelLineView
    {
        void ResetAllNodes();
        void ShowLevelNode(LevelNodeModel model, Action<LevelNodeModel> onClickCallback);
        void UpdateBtnPlay(int difficulty, Action onClick);
        void SetWinstreak(int winstreak);
        void UpdateRemainThings();
    }
}