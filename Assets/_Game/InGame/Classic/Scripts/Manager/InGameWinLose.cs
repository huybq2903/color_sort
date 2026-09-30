using Falcon.Shared.BaseInGame;

namespace Falcon.InGame.Core
{
    public class InGameWinLose : AWinLoseManager
    {
        protected override bool IsWin()
        {
            return false;
        }

        protected override bool IsLose()
        {
            return false;
        }
    }
}