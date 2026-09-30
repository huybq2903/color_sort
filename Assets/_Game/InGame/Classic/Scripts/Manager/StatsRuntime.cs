/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-05
 */

using System.Collections.Generic;
using Falcon.Shared.BaseInGame;

namespace Falcon.InGame.Log
{
    /// <summary>Số liệu của lượt chơi, nằm trong LevelRuntime nên resume không mất.</summary>
    [PropertyRuntimeType("stats")]
    public class StatsRuntime : PropertyRuntime
    {
        /// <summary>Tổng giây đã chơi, cộng dồn qua các lần resume.</summary>
        public int durationPlayed;

        /// <summary>Số vàng đã tiêu trong màn (mua booster…).</summary>
        public int coinSpend;

        /// <summary>Số lần revive.</summary>
        public int amountMoreTime;

        /// <summary>Số lần dùng từng loại booster, key là BoosterType.</summary>
        public Dictionary<string, int> numUsedBoosters = new();

        /// <summary>Tiến độ màn (game con tự cộng), dùng làm score khi log kết quả.</summary>
        public int score;
    }
}
