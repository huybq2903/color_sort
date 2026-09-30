using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.Network;
using Falcon.Modules.Level;
using UnityEngine;
using System;
/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-18
namespace Falcon.Modules.Level.Level4Profile
{
    public class EventBus4ProfileStatistic
    {
        [RuntimeInitializeOnLoadMethod]
        public static void Register()
        {
            GameEvent<int>.Register(Const.OnProfileClick, OnProfileClick, null);
        }
    
        private static void OnProfileClick(int code)
        {
            new CSLevelInfo4ProfileReq(code).AddSCListener<SCLevelInfo4ProfileRsp>((message, timeout, success) =>
            {
                if (success)
                {
                    GameEvent<(Sprite icon, string id, int value)>.Emit(Const.OnProfileStatisticUpdate + "_highestlevel", (Resources.Load<Sprite>("icon-highest-level"), "Level", message.max_level));
                    GameEvent<(Sprite icon, string id, int value)>.Emit(Const.OnProfileStatisticUpdate + "_longeststreak", (Resources.Load<Sprite>("icon-longest-streak"), "Streak", message.win_strike));
                    GameEvent<(Sprite icon, string id, int value)>.Emit(Const.OnProfileStatisticUpdate + "_oneshotwin", (Resources.Load<Sprite>("icon-one-shot-win"), "First Try Wins", message.one_time_shot));
                }
            }
            ).Send();
            
        }
    }
}

