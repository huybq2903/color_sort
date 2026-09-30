/*
     * Author: minhddv
     * Email: minhddv@falcongames.com
     * Company: Falcon Games
     * Date: 2025-06-23
*/

using System;
using System.Collections.Generic;

namespace Falcon.Modules.UI.Level.Runtime
{
    public class LevelLineModel
    {
        private readonly int _numberOfLevels;
        private readonly int _currentLevel;
        private readonly Func<int, int> _getDifficulty;
        private readonly Func<int, bool> _hasRewards;

        public int WinStreak { get; }

        public LevelLineModel(int numberOfLevels, int currentLevel, int winStreak, Func<int, int> getDifficulty, Func<int, bool> hasRewards)
        {
            _numberOfLevels = numberOfLevels;
            _currentLevel = currentLevel;
            WinStreak = winStreak;
            _getDifficulty = getDifficulty;
            _hasRewards = hasRewards;
        }

        public List<LevelNodeModel> GetLevelNodes()
        {
            var list = new List<LevelNodeModel>();
            for (int i = _currentLevel; i < _currentLevel + _numberOfLevels; i++)
            {
                list.Add(new LevelNodeModel(i,
                    i == _currentLevel,
                    _getDifficulty(i),
                    _hasRewards(i)));
            }

            return list;
        }
    }
}