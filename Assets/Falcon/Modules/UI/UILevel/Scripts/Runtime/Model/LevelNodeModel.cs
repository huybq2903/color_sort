/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
*/

namespace Falcon.Modules.UI.Level.Runtime
{
    public class LevelNodeModel
    {
        public readonly int index;
        public readonly bool isCurrent;
        public readonly int difficulty;
        public readonly bool hasReward;

        public LevelNodeModel(int index, bool isCurrent, int difficulty, bool hasReward)
        {
            this.index = index;
            this.isCurrent = isCurrent;
            this.difficulty = difficulty;
            this.hasReward = hasReward;
        }
    }
}