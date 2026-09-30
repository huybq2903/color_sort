/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2026-09-05
 */

using System;
using System.Collections.Generic;
using Falcon.Helpers.Devkit;
using JetBrains.Annotations;
using UnityEngine.Scripting;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.Core.BigData
{
    /// <summary>
    /// Bản tin CỤM exposure UI — một dòng = <see cref="count"/> lần (surface × action) trong một
    /// stretch, flush lúc app pause (khuôn cụm banner/multi-floor). Grain per-lần-hiện CẤM lên
    /// wire (chốt owner 05/09 — volume không chặn trên); CTR ngày phía server =
    /// <c>sum(count | click) / sum(count | impression)</c>.
    /// <br/>⚠ Event id chờ loader ký. Cụm KHÔNG persist qua kill — telemetry exposure, mất
    /// stretch dở chấp nhận được (cùng phán quyết với cụm multi-floor).
    /// </summary>
    [Serializable]
    public class FUiImpressionLog : AFalconLog
    {
        /// <summary>Bề mặt UI nào — vocab của từng game/event ("race_event_entry", "race_event_popup"...).</summary>
        public string surfaceId;

        /// <summary>Hành vi — <see cref="FUiAction"/>: "impression" / "click".</summary>
        public string uiAction;

        /// <summary>Số lần đã gộp trong dòng này — server đếm bằng sum(count), không phải count(*).</summary>
        public int count;

        [FKey(RemoveIfNull = true)] [CanBeNull]
        public Dictionary<string, object> extraMeta;

        [Preserve]
        public FUiImpressionLog()
        {
        }

        public FUiImpressionLog(UiExposureState.Entry entry)
        {
            surfaceId = entry.surfaceId;
            uiAction = entry.uiAction;
            count = entry.count;
            extraMeta = entry.extraMeta;
        }

        public override string Event => "f_sdk_ui_impression";

        public override Dictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();
            dictionary.Remove(nameof(extraMeta));
            if (extraMeta == null) return dictionary;

            foreach (var (key, value) in extraMeta) dictionary.PutIfAbsentAndNotNull(key, value);
            return dictionary;
        }
    }
}
