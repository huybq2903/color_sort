/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-08
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    public abstract class ALevelParamV2 : FParam 
    {
        public string playTurnId;
        public int currentLevel;

        [FKey(RemoveIfNull = true)] [CanBeNull]
        public string currentLevelId;

        [NotNull] public string difficulty = UNKNOWN;

        // elo KHÔNG còn ở đây: nó tả NGƯỜI CHƠI chứ không tả lượt chơi (luật chủ thể §H6), và
        // là state quan-sát-được nên hợp đồng xếp vào nhóm "gửi khi ĐỔI" — nay đi bằng bản tin
        // f_sdk_user_label qua FalconBigDataController.Level.SetUserElo(...). Gửi kèm mọi level
        // event là nhân bản một sự thật của user lên stream dày nhất hệ.


        // movesLimit / timeLimitSec KHÔNG còn ở đây: chúng tả BẢN THIẾT KẾ MÀN chứ không tả lượt
        // chơi, mà luật container §H6 cấm thả attribute của chủ thể ngoại xuống flat. Nay chúng đi
        // trong bundle levelLabels dưới tên hợp đồng (FLevelLabelKey) — API nhận vào không đổi,
        // SDK tự bỏ đúng nhà. Xem EntityLifecycle-Design.md §5c.

        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> extraMeta = null;

        public bool comeBackAfterFirstPass = false;
        
        [FKey(Ignore = true)] public bool autoCheckComeBackAfterFirstPass = true;

        public override void CorrectValues()
        {
            currentLevel = CheckNumberNonNegative(currentLevel, "currentLevel");
            difficulty = CheckNonBlank(difficulty, "difficulty");
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            dictionary.PutIfAbsent("status", Status);
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }

        public virtual string LevelId => currentLevel + "_Difficulty_" + difficulty;
        
        public abstract LevelStatus Status { get; }
    }
}