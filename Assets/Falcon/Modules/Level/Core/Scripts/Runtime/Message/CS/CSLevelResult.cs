/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-10


using System.Collections.Generic;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.BigData;
using Falcon.Modules.Core.Network;

namespace Falcon.Modules.Level.Core
{
    [FAMessage("cs_level_result")]
    public class CSLevelResult : CSMessageWaitLoginSuccess
    {
        public int level;
        public string md5LevelContent;
        public string levelParam;
        public int numBooster;
        public bool win;
        public int time;
        public string currencyCode;
        public double inApp;
        public Dictionary<string, int> booster2Number;
        public int score;
        public int totalScore;
        public int coinSpend;
        public int numberBuyMoreTime;
        public int numberMove;
        public string info;

        /// <summary>
        /// Tham số thêm của game cho ván này — bản THÔ đúng như game truyền (không trộn
        /// coinSpend/score/info vào, mấy cái đó đã có field riêng ở trên). Null = không gửi.
        /// </summary>
        public Dictionary<string, object> extraMeta;
        public CSLevelResult()
        {
        }

        public CSLevelResult(int level, string md5LevelContent, string levelParam, int numBooster, bool win, int time,
            string currencyCode, double inApp, Dictionary<string, int> booster2Number, int score, int totalScore,
            int coinSpend, int numberBuyMoreTime, int numberMove, string info)
        {
            this.level = level;
            this.md5LevelContent = md5LevelContent;
            this.levelParam = levelParam;
            this.numBooster = numBooster;
            this.win = win;
            this.time = time;
            this.currencyCode = currencyCode;
            this.inApp = inApp;
            this.booster2Number = booster2Number;
            this.score = score;
            this.totalScore = totalScore;
            this.coinSpend = coinSpend;
            this.numberBuyMoreTime = numberBuyMoreTime;
            this.numberMove = numberMove;
            this.info = info;
        }
    }
}