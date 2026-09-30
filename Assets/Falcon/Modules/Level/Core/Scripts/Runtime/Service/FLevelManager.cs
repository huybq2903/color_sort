/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-11


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.BigData;
using UnityEngine;

namespace Falcon.Modules.Level.Core
{
    public class FLevelManager : MySingleton<FLevelManager>
    {
        private const string SERVER_LEVEL_PATH = "/falcon/server_levels";
        private const string LEVEL_DATA_EXTENSION = ".data";
        private const string LEVEL_PARAM_EXTENSION = ".param";
        private const string LEVEL_DIFFICULTY_EXTENSION = ".difficulty";
        private const string LEVEL_AB_INFORMATION = "falcon.modules.level.core_level_ab_information";

        public const int LEVEL_DIFFICULTY_EASY = 0;
        public const int LEVEL_DIFFICULTY_HARD = 1;
        public const int LEVEL_DIFFICULTY_EXTREME = 2;

        public const string EVENT_BUS_LEVEL_START = "event_bus_level_start";
        public const string EVENT_BUS_LEVEL_READY = "event_bus_level_ready";
        public const string EVENT_BUS_LEVEL_COMPLETE = "event_bus_level_complete";
        public const string EVENT_BUS_LEVEL_FAILED = "event_bus_level_failed";
        public const string EVENT_BUS_IAP_SUCCESS = "falcon.modules.iap.purchase_success";
        public const string EVENT_GET_LEVEL = "falcon.modules.core.gamedata.get_level";
        private readonly IFLevelProvider _levelProvider;

        private string _currencyCode = "";
        private double _price;

        private string currentLevelData = "";
        private string currentLevelParam = "";
        private string currentMd5LevelData = "";
        public bool isTestingLevel = false;
        private long levelStartTime;

        public FLevelManager(IFLevelProvider levelProvider)
        {
            _levelProvider = levelProvider;
        }

        /// <summary>
        /// playTurnId của lượt chơi ĐANG MỞ — do BigData quản lý tập trung (LevelTurnState),
        /// null khi không có lượt nào mở. Level log không cần tự set field này nữa, cache tự điền.
        /// </summary>
        public string PlayTurnId => LevelTurnService.Instance.OpenPlayTurnId;
        
        public string CurrentMd5LevelDataParam { get; private set; }

        public string FilterID => AbLevelInfo.Instance.filterID;
        public string AbVariant => AbLevelInfo.Instance.abVariant;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void RegisterEventBus()
        {
            GameRequest<int>.Register(EVENT_GET_LEVEL, () => LevelData.Instance.level);
        }

        public void SaveLevels(
            Dictionary<int, string> level2Data,
            Dictionary<int, string> level2Param,
            Dictionary<int, int> level2Difficulty)
        {
            if (level2Data != null)
                foreach (var levelContent in level2Data)
                {
                    var level = levelContent.Key;
                    var content = levelContent.Value;
                    Save(level, content, LEVEL_DATA_EXTENSION);
                }

            if (level2Param != null)
                foreach (var param in level2Param)
                {
                    var level = param.Key;
                    var paramTxt = param.Value;
                    Save(level, paramTxt, LEVEL_PARAM_EXTENSION);
                }

            if (level2Difficulty != null)
                foreach (var difficulty in level2Difficulty)
                {
                    var level = difficulty.Key;
                    var levelDifficulty = difficulty.Value;
                    Save(level, levelDifficulty, LEVEL_DIFFICULTY_EXTENSION);
                }
        }

        public void SaveAbInformation(string filterID, string abVariant)
        {
            AbLevelInfo.Instance.filterID = filterID;
            AbLevelInfo.Instance.abVariant = abVariant;
            AbLevelInfo.Instance.Save();

            GameEvent<(string filterID, string abVariant)>.Emit(LEVEL_AB_INFORMATION, (filterID, abVariant));
        }

        private void Save(int level, object content, string extension)
        {
            var folderPath = Application.persistentDataPath + SERVER_LEVEL_PATH;
            var fileName = level + extension;
            var fullPath = Path.Combine(folderPath, fileName);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            if (content is string str && string.IsNullOrEmpty(str))
                File.Delete(fullPath);
            else
                File.WriteAllText(fullPath, "" + content);
        }

        public string GetLevelData(int level)
        {
            var filePath = GetFilePath(level, LEVEL_DATA_EXTENSION);
            if (File.Exists(filePath))
            {
                var levelData = File.ReadAllText(filePath);
                if (_levelProvider.IsValidLevelData(levelData))
                    return levelData;
            }

            try
            {
                return _levelProvider.GetLevelData(level);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return "";
            }
        }

        public string GetLevelDataMd5(int level)
        {
            return Md5Utils.GetMd5First5Char(GetLevelData(level));
        }

        public string GetLevelParam(int level)
        {
            var filePath = GetFilePath(level, LEVEL_PARAM_EXTENSION);
            if (File.Exists(filePath)) return File.ReadAllText(filePath);

            try
            {
                return _levelProvider.GetLevelParam(GetLevelData(level));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return "";
            }
        }

        public int GetLevelDifficulty(int level)
        {
            var filePath = GetFilePath(level, LEVEL_DIFFICULTY_EXTENSION);
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                if (int.TryParse(content, out var difficulty)) return difficulty;
            }

            return LEVEL_DIFFICULTY_EASY;
        }

        private string GetFilePath(int level, string fileExtension)
        {
            var folderPath = Application.persistentDataPath + SERVER_LEVEL_PATH;
            var fileName = level + fileExtension;
            return Path.Combine(folderPath, fileName);
        }

        public void VerifyLevel(string levelData, Action<bool> onResult)
        {
            var md5LevelData = Md5Utils.GetMd5First5Char(levelData);

            new CSLevelVerifyReq(md5LevelData)
                .AddSCListener<SCLevelVerifyRsp>((message, timeout, success) =>
                {
                    // Ví dụ: xử lý kết quả xác thực ở đây
                    if (!success || timeout)
                        onResult?.Invoke(false);
                    else
                        onResult?.Invoke(true);
                })
                .Send();
        }

        public void OnLevelReady()
        {
            LevelData.Instance.startCurrentLevel = true;
            var level = LevelData.Instance.level;
            GameEvent<int>.Emit(EVENT_BUS_LEVEL_READY, level);

            var csLevelReady = new CSLevelReady(level);

            currentLevelData = GetLevelData(level);
            currentMd5LevelData = csLevelReady.md5LevelData;
            currentLevelParam = csLevelReady.levelParam;
            CurrentMd5LevelDataParam = Md5Utils.GetMd5First5Char(currentLevelData + currentLevelParam);
            csLevelReady.Send();
        }

        /// <param name="param">
        /// Kênh LEGACY (đời PuzzleLevelLog): object param của game, ToDictionary rồi chảy vào
        /// extraMeta của log BigData. Code mới dùng <paramref name="extraMeta"/> cho thẳng.
        /// </param>
        /// <param name="extraMeta">
        /// Tham số thêm của game cho lần chơi — đi CẢ hai đường: extraMeta của log BigData
        /// (server lưu event_extra_props, đăng ký hợp đồng sau là cook hồi tố được) và field
        /// extraMeta trên CSLevelStart cho game server. Trùng key với <paramref name="param"/>
        /// thì dict này thắng. Key tự đặt — tra hợp đồng trước khi đặt tên, đừng trùng cột sẵn có
        /// (luật §H1; đường Dictionary thô không qua guard tự động).
        /// </param>
        public void OnLevelStart(
            FParam param = null, int? movesLimit = null, int? timeLimitSec = null,
            Dictionary<string, object> extraMeta = null)
        {
            levelStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var level = LevelData.Instance.level;
            GameEvent<int>.Emit(EVENT_BUS_LEVEL_START, level);
            LevelData.Instance.playTimes++;
            LevelData.Instance.UpdateToServer();
            // IAP tính theo từng level: reset phần cộng dồn của level trước, không là thành iap của cả phiên
            _price = 0;
            _currencyCode = "";
            GameEvent<(string currencyCode, double price)>.Register(EVENT_BUS_IAP_SUCCESS, OnPurchaseSuccess, null);
            new CSLevelStart(level, currentMd5LevelData, currentLevelParam) { extraMeta = extraMeta }.Send();

            // `param` là kênh legacy từ đời PuzzleLevelLog — refactor 4938a6754 từng làm rơi dây,
            // game truyền vào bị NUỐT IM LẶNG. Nối lại đúng nghĩa cũ: chảy vào extraMeta của log,
            // cùng khuôn OnLevelResult; dict tường minh thắng khi trùng key.
            var mergedMeta = param == null ? extraMeta : param.ToDictionary();
            if (param != null && extraMeta != null)
                foreach (var (key, value) in extraMeta)
                    mergedMeta.Put(key, value);

            // Đi qua cửa chuẩn Level.OnStart: movesLimit/timeLimitSec là config của MÀN (§H6) nên
            // đứng ngoài param — cửa này tự khai bundle nhãn TRƯỚC khi enqueue, hết phải tự nhớ
            // thứ tự SetLevelConfig-rồi-Send. playTurnId do BigData sinh + cache khi log Start đi qua.
            FalconBigDataController.Level.OnStart(new LevelStartParamV2
            {
                currentLevel = level,
                currentLevelId = CurrentMd5LevelDataParam,
                difficulty = GetLevelDifficulty(level).ToString(),
                extraMeta = mergedMeta
            }, movesLimit, timeLimitSec);
        }

        public void SendLevelPlayingInfo(string info)
        {
            var level = LevelData.Instance.level;
            var levelData = GetLevelData(level);
            var md5LevelData = Md5Utils.GetMd5First5Char(levelData);
            var deltaTime = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - levelStartTime) / 1000;
            if (string.IsNullOrEmpty(currentLevelParam))
                currentLevelParam = GetLevelParam(level);
            new CSLevelPlayingInfo(level, md5LevelData, currentLevelParam, deltaTime, info).Send();
        }

        public void LevelHeartBeat(LevelHeartBeatParamV2 param)
        {
            var level = LevelData.Instance.level;
            param.currentLevel = level;
            param.currentLevelId = CurrentMd5LevelDataParam;
            param.difficulty = GetLevelDifficulty(level).ToString();

            FalconBigDataController.Level.OnHeartbeat(param);
        }

        /// <param name="extraMeta">
        /// Tham số thêm của game cho ván này — đi CẢ hai đường như bên OnLevelStart. Trên log
        /// BigData, key game truyền tường minh THẮNG các key suy ra từ info/param cũ khi trùng.
        /// </param>
        public void OnLevelResult(
            bool win, int time, Dictionary<string, int> booster2Number, int score, int totalScore, int coinSpend,
            int numberBuyMoreTime, int numberMove, FParam param = null, string info = "",
            LevelFailReason? failReason = null, Dictionary<string, object> extraMeta = null)
        {
            var level = LevelData.Instance.level;

            if (win)
            {
                GameEvent<(string currentLevel, string timePlayed, int score)>.Emit(EVENT_BUS_LEVEL_COMPLETE,
                    ("" + level, "" + time, score));
                LevelData.Instance.level++;
                LevelData.Instance.startCurrentLevel = false;
                LevelData.Instance.playTimes = 0;
            }
            else
            {
                GameEvent<(string currentLevel, string failCount)>.Emit(EVENT_BUS_LEVEL_FAILED,
                    ("" + level, "" + LevelData.Instance.playTimes));
            }

            LevelData.Instance.UpdateToServer();

            var numBooster = booster2Number != null ? booster2Number.Values.Sum() : 0;
            //event bus unregister
            GameEvent<(string currencyCode, double price)>.Unregister(EVENT_BUS_IAP_SUCCESS, OnPurchaseSuccess, null);
            if (string.IsNullOrEmpty(currentLevelParam))
                currentLevelParam = GetLevelParam(level);
            new CSLevelResult(level, currentMd5LevelData, currentLevelParam, numBooster, win, time, _currencyCode,
                _price,
                booster2Number, score, totalScore, coinSpend, numberBuyMoreTime, numberMove, info)
                { extraMeta = extraMeta }.Send();

            // Log BigData gom meta từ BA nguồn theo thứ tự thắng tăng dần: param cũ + info json
            // (đường legacy) rồi tới Dictionary extraMeta game truyền tường minh — cái tường minh
            // đè cái suy ra. Cột hợp đồng thì không nguồn nào đè nổi (extraMeta merge PutIfAbsent
            // ở tầng param).
            var mergedMeta = (param?.ToDictionary() ?? new Dictionary<string, object>())
                .Put("coinSpend", coinSpend)
                .Put("totalScore", totalScore);
            try
            {
                foreach (var (key, value) in info.JsonToObj<Dictionary<string, object>>())
                {
                    mergedMeta.PutIfAbsentAndNotNull(key, value);
                }
            }
            catch (Exception)
            {
                mergedMeta.Put("info", info);
            }

            if (extraMeta != null)
                foreach (var (key, value) in extraMeta)
                    mergedMeta.Put(key, value);
            
            //coi việc buyMoreTime là 1 loại booster 
            if (booster2Number != null) booster2Number["buyMoreTime"] = numberBuyMoreTime;

            SendLevelLog(win, time, booster2Number, score, totalScore, numberMove, mergedMeta, level, failReason);
        }

        private void SendLevelLog(
            bool win, int time, Dictionary<string, int> booster2Number,
            int score, int totalScore, int numberMove,
            Dictionary<string, object> extraMeta, int level,
            LevelFailReason? failReason = null
        )
        {
            // playTurnId do BigData cache tự điền (cùng id với log Start của lượt này)
            if (win)
            {
                FalconBigDataController.Level.OnPass(new LevelPassParamV2
                {
                    currentLevel = level,
                    currentLevelId = CurrentMd5LevelDataParam,
                    difficulty = GetLevelDifficulty(level).ToString(),
                    duration = TimeSpan.FromSeconds(time),
                    boostersUsed = booster2Number,
                    score = score,
                    movesUsed = numberMove,
                    extraMeta = extraMeta
                });
            }
            else
            {
                var failParam = new LevelFailParamV2
                {
                    currentLevel = level,
                    currentLevelId = CurrentMd5LevelDataParam,
                    difficulty = GetLevelDifficulty(level).ToString(),
                    duration = TimeSpan.FromSeconds(time),
                    boostersUsed = booster2Number,
                    score = score,
                    movesUsed = numberMove,
                    extraMeta = extraMeta,
                    failReason = failReason
                };
                // Game không có hệ điểm (totalScore = 0) thì để levelProgress mặc định thay vì
                // DivideByZeroException — đúng họ bug đã vá ở ALevelHeartBeatService 1.1.4
                if (totalScore > 0) failParam.levelProgress = score * 100 / totalScore;
                FalconBigDataController.Level.OnFail(failParam);
            }
        }

        private void OnPurchaseSuccess((string currencyCode, double price) obj)
        {
            _currencyCode = obj.currencyCode;
            _price += obj.price;
        }

        internal void ForcePlayLevel(string levelData)
        {
            Save(LevelData.Instance.level, levelData, LEVEL_DATA_EXTENSION);
        }

        // Trả về mảng các level đã được tải từ Server về, theo thứ thự tăng dần
        // thường sẽ tuần tự từ 1-> n nhưng cũng có thể ngắt quãng
        // Ví dụ: [1,3,6,7,8,12,15,17]
        public int[] GetAllLevels()
        {
            var folderPath = Application.persistentDataPath + SERVER_LEVEL_PATH;
            if (!Directory.Exists(folderPath)) return Array.Empty<int>();

            var files = Directory.GetFiles(folderPath, "*" + LEVEL_DATA_EXTENSION);
            var levels = new List<int>();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (int.TryParse(fileName, out var level)) levels.Add(level);
            }

            return levels.OrderBy(l => l).ToArray();
        }
    }
}