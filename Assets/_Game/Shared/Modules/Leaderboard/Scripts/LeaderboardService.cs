// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-08-12

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using Falcon.Shared.Common.Time;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Shared.Leaderboard
{
    /// <summary>Bọc CS/SC thành UniTask, cache theo category_type.</summary>
    public class LeaderboardService : IInitialize
    {
        private const int TIMEOUT = 10;

        private readonly Dictionary<string, LbPage> _pages = new();
        private readonly Dictionary<string, long> _endTimes = new();

        private const string EVENT_JOIN_CLAN = "falcon.modules.clan.join_clan";
        private const string EVENT_LEAVE_CLAN = "falcon.modules.clan.leave_clan";
        private const string EVENT_GET_USER_CLAN = "falcon.modules.clan.get_user_clan";

        public void OnInitialize()
        {
            // Chua bat module clan thi khong ai emit, MyClanCode giu 0 -> IsMe luon false
            GameEvent<(int, string, Sprite)>.Register(EVENT_JOIN_CLAN, d => LbClanEntry.MyClanCode = d.Item1, null);
            GameEvent.Register(EVENT_LEAVE_CLAN, () => LbClanEntry.MyClanCode = 0, null);
        }

        /// <summary>Hỏi module clan xem mình đang ở clan nào; không có module thì callback không chạy.</summary>
        private void RefreshMyClanCode()
        {
            GameEvent<(int, Action<int, string, Sprite>)>.Emit(EVENT_GET_USER_CLAN,
                (AccountManager.Instance.Code, (code, _, _) => LbClanEntry.MyClanCode = code));
        }

        /// <summary>Bắn khi kết nối lên/xuống; LbSessionListener gọi vào đây.</summary>
        public event Action OnConnectionChanged;

        private bool _connected;

        internal void SetConnected(bool value)
        {
            if (_connected == value) return;
            _connected = value;
            OnConnectionChanged?.Invoke();
        }

        // ----- Config, do SCGetConfigLB nuôi -----

        private int _levelUnlock = -1;
        private int _refreshInterval;
        private int _refreshByUserInterval = 10;
        private long _nextFetchSecond;
        private long _nextManualRefreshSecond;

        /// <summary>SCGetConfigLB gọi vào đây khi config về.</summary>
        internal void SetConfig(int levelUnlock, int refreshInterval, int refreshByUserInterval)
        {
            _levelUnlock = levelUnlock;
            _refreshInterval = refreshInterval;
            _refreshByUserInterval = refreshByUserInterval;

            // Config về cũng là lúc Ready đổi từ false sang true
            OnConnectionChanged?.Invoke();
        }

        /// <summary>Xin config; gọi một lần sau khi vào game, bản tin tự chờ đăng nhập xong.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RequestConfig() => new CSGetConfigLB().Send();

        // LevelUnlock < 0 nghĩa là config chưa về, chưa đủ để hiện bảng
        public bool Ready => _connected && _levelUnlock >= 0;
        public int LevelUnlock => _levelUnlock;
        public bool Unlocked => Ready && CurrentLevel >= _levelUnlock;

        private int CurrentLevel => GameRequest<int>.Request(GameKeys.GET_LEVEL);

        public bool CanManualRefresh() => WrapperTime.CurrentSecond >= _nextManualRefreshSecond;

        /// <summary>Xoá cache và hẹn lại 2 mốc thời gian theo config server.</summary>
        public void Invalidate()
        {
            _pages.Clear();
            _endTimes.Clear();
            _nextFetchSecond = WrapperTime.CurrentSecond + _refreshInterval;
            _nextManualRefreshSecond = WrapperTime.CurrentSecond + _refreshByUserInterval;
        }

        /// <summary>entryType quyết định shape đọc từ server: LbPlayerEntry hay LbClanEntry.</summary>
        public async UniTask<LbPage> Fetch(string category, string type, Type entryType, CancellationToken ct)
        {
            if (entryType == null)
            {
                Debug.LogWarning($"[Leaderboard] {category} chưa gán prefab dòng, không biết parse ra kiểu gì.");
                return null;
            }

            if (entryType == typeof(LbClanEntry) && LbClanEntry.MyClanCode == 0) RefreshMyClanCode();

            if (WrapperTime.CurrentSecond >= _nextFetchSecond) Invalidate();

            var key = $"{category}_{type}";
            if (_pages.TryGetValue(key, out var cached)) return cached;

            var tcs = new UniTaskCompletionSource<SCGetLBData>();
            new CSGetLBData { leaderboardCategoryStr = category, leaderboardTypeStr = type }
                .AddSCListener<SCGetLBData>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null)
            {
                Debug.LogWarning($"[Leaderboard] fetch {key} thất bại (timeout hoặc lỗi).");
                return null;
            }

            var page = Parse(sc, entryType);
            if (page != null) _pages[key] = page;
            return page;
        }

        /// <summary>Mốc kết thúc mùa giải, tính bằng giây unix để đưa thẳng cho WrapperTime. 0 nếu không lấy được.</summary>
        public async UniTask<long> FetchEndSecond(string category, CancellationToken ct)
        {
            if (_endTimes.TryGetValue(category, out var cached)) return cached;

            var tcs = new UniTaskCompletionSource<SCGetCategoryDataLB>();
            new CSGetCategoryDataLB(category)
                .AddSCListener<SCGetCategoryDataLB>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null) return 0;

            var end = WrapperTime.CurrentSecond + sc.timeLeft / 1000;
            _endTimes[category] = end;
            return end;
        }

        private static LbPage Parse(SCGetLBData sc, Type entryType)
        {
            var page = new LbPage();
            try
            {
                if (!string.IsNullOrEmpty(sc.rows))
                {
                    var listType = typeof(List<>).MakeGenericType(entryType);
                    if (JsonConvert.DeserializeObject(sc.rows, listType) is IEnumerable<LbEntry> rows)
                        page.entries.AddRange(rows);
                }

                if (!string.IsNullOrEmpty(sc.bonusData))
                {
                    var me = JObject.Parse(sc.bonusData)["myData"];
                    if (me != null && me.Type != JTokenType.Null) page.me = me.ToObject(entryType) as LbEntry;
                }

                // Tách profileData 1 lần ở đây, row chỉ việc đọc avatarId/frameId
                foreach (var e in page.entries) (e as LbPlayerEntry)?.ParseProfile();
                (page.me as LbPlayerEntry)?.ParseProfile();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Leaderboard] parse lỗi: {e}");
                return null;
            }

            return page;
        }
    }

    /// <summary>
    /// Theo dõi kết nối cho riêng module này. FNetManager tự quét và tạo instance qua reflection
    /// nên không phải đăng ký tay.
    /// </summary>
    public class LbSessionListener : ISessionListener
    {
        private static LeaderboardService Service => Center.GetOrCreate<LeaderboardService>();

        public void OnFirstSession() => Service.SetConnected(true);

        public void OnSessionReset() => Service.SetConnected(true);

        public void OnChannelDisconnected(FChannel channel) => Service.SetConnected(false);
    }
}
