/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.RemoteConfigCms;
using UnityEngine;
using Falcon.Shared.Common;

public class GameConfigCms : IFalconConfigCms
{
    public int level_unlock_booster_1 = 3;
    public int level_unlock_booster_2 = 5;
    public int level_unlock_booster_3 = 7;
    public int price_booster_1 = 300;
    public int price_booster_2 = 300;
    public int price_booster_3 = 300;
    public int default_quantity_booster_1 = 2;
    public int default_quantity_booster_2 = 2;
    public int default_quantity_booster_3 = 2;

    public int levelInitMediation = 2;
    public int levelStartBanner = 999;
    public int levelStartInters = 31;
    public int timeToOpenRemoveAdsAfterInters = 3;
    public int timeShowInterAgain = 120;
    public int timeShowInterAfterPay = 600;

    public int goldWin_0 = 50;
    public int goldWin_1 = 100;
    public int goldWin_2 = 200;
    public int rate_ads_gold_win = 2;

    public int price_revive = 100;
    public int price_refill_lives = 100;
    public int level_start_ads_win = 10;
    public int level_start_ads_revive = 10;
    public int level_start_ads_buy_booster = 10;
    public int level_start_ads_refill_lives = 10;
    public int ads_amount_refill_lives = 1;
}

/// Đọc remote config theo key, có memo cache, tự invalidate khi config update từ net.
public static class GameConfig
{
    private static int _ver;

    private static class Cache<T>
    {
        internal static readonly Dictionary<string, (int Ver, T Val)> Map = new();
    }

    /// Gọi từ StartBehaviour.InitCallback().
    public static void Init()
    {
        // Domain reload off -> static còn sống giữa 2 lần Play, bump để bỏ cache session cũ.
        _ver++;
        FConfig.OnUpdateFromNet -= Invalidate;
        FConfig.OnUpdateFromNet += Invalidate;

        GameRequest<string, int>.Register(GameKeys.GET_REMOTE_CONFIG, key => Get(key, 1));
        GameRequest<(string, int), int>.Register(GameKeys.GET_REMOTE_CONFIG, req => Get(req.Item1, req.Item2));
    }

    private static void Invalidate() => _ver++;

    public static T Get<T>(string key, T fallback = default)
    {
        // Edit mode: FConfig dựng cả singleton graph runtime (MainGameObj + NoLazy) -> loop editor.
        if (!Application.isPlaying) return fallback;

        if (Cache<T>.Map.TryGetValue(key, out var e) && e.Ver == _ver) return e.Val;

        try
        {
            // Miss thì không cache: cùng key khác fallback ở call site khác sẽ ăn nhầm giá trị.
            if (!FConfig.ConfigEntries.TryGetValue(key, out var v) || v == null) return fallback;

            var val = (T)Convert.ChangeType(v, typeof(T), CultureInfo.InvariantCulture);
            Cache<T>.Map[key] = (_ver, val);
            return val;
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
