/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    [Primary]
    public class ServerPlayerSessionRepository : IFPlayerSessionRepository, ITerminal, IInit
    {
        private readonly ReservePlayerSessionRepository _reserveRepository;
        private readonly ITimeRepository _timeRepository;

        public ServerPlayerSessionRepository(
            ReservePlayerSessionRepository reserveRepository, ITimeRepository timeRepository)
        {
            _reserveRepository = reserveRepository;
            _timeRepository = timeRepository;
            RetentionChanged = _reserveRepository.RetentionChanged;
        }

        public long GetModeTotalSec(string gameMode)
        {
            var reserveTotal = _reserveRepository.GetModeTotalSec(gameMode);
            var serverModes = PlaySessionData.Instance.gameModeToTotalSec;
            // TryGetValue chứ KHÔNG GetValueOrDefault-rồi-??: bản cũ coi dict RỖNG (account mới,
            // sync trả sổ trắng) là "có sổ, tổng 0" nên fallback về sổ nhà chết hẳn — án
            // total_play_time 29→0 giữa phiên (log 13/08). Thiếu DÒNG cũng phải mở sổ nhà,
            // không riêng gì thiếu SỔ.
            if (serverModes == null || !serverModes.TryGetValue(gameMode, out var serverTotal))
                return reserveTotal;

            // Counter chỉ được đi lên → sổ nào số LỚN HƠN là sổ đúng: reserve chỉ vượt được sổ
            // server khi sổ server từng bị reset (bug cũ / sổ trắng đè). Max ở đây là máy TỰ LÀNH
            // cho các máy ngoài fleet đã dính — không cần migration. Chiều đa-thiết-bị vẫn đúng:
            // server hợp lệ lớn hơn reserve (chơi máy khác) thì max giữ nguyên server.
            return Math.Max(serverTotal, reserveTotal);
        }

        public long IncreaseModeTotalSec(string gameMode, long seconds)
        {
            var reserveTotal = _reserveRepository.IncreaseModeTotalSec(gameMode, seconds);
            PlaySessionData.Instance.gameModeToTotalSec ??= new Dictionary<string, long>();
            var serverModes = PlaySessionData.Instance.gameModeToTotalSec;
            // Seed/hàn từ SỔ NHÀ chứ không từ 0: bản cũ tạo entry mới = 0 + seconds, vứt lịch sử
            // reserve — hai sổ lệch vĩnh viễn và wire mang số nhỏ. Max với reserve (đã cộng
            // seconds ở trên) thì entry thiếu hay tụt đều hội tụ về sự thật ở lần ghi kế.
            var serverTotal = serverModes.TryGetValue(gameMode, out var time) ? time + seconds : 0;
            return serverModes[gameMode] = Math.Max(serverTotal, reserveTotal);
        }

        public long FirstLogInMillis =>
            PlaySessionData.Instance.firstLogInMillis ?? _reserveRepository.FirstLogInMillis;

        // Max chứ không ??: activeDays phía server chỉ được cộng SAU login (UpdateRemoteData) —
        // login không xong (editor, mạng xấu) thì blob đứng im trong khi sổ nhà vẫn tự cộng mỗi
        // ngày chơi; blob khác-null đè sổ nhà là activeDays kẹt 1 giữa retentionDay 13 (log 13/08).
        public int ActiveDays => PlaySessionData.Instance.activeDays is { } serverDays
            ? Math.Max(serverDays, _reserveRepository.ActiveDays)
            : _reserveRepository.ActiveDays;
        public int SessionId => PlaySessionData.Instance.sessionId ?? _reserveRepository.SessionId;
        public bool RetentionChanged { get; private set; }

        public async Task Init(CancellationToken cancellationToken = default)
        {
            if (RemoteUncertain())
            {
                if(!await AccountLoginListener.WaitLogin()) return;
                if (RemoteUncertain())
                    OverwriteRemoteData();
                else
                    UpdateRemoteData();
            }
            else
            {
                UpdateRemoteData();
            }
        }

        public void OnPostStop()
        {
            if (!RemoteUncertain()) PlaySessionData.Instance.UpdateToServer();
        }

        private static bool RemoteUncertain()
        {
            var playerGeneralData = PlaySessionData.Instance;
            return playerGeneralData.gameModeToTotalSec == null
                   || playerGeneralData.firstLogInMillis == null
                   || playerGeneralData.activeDays == null
                   || playerGeneralData.sessionId == null
                   || playerGeneralData.lastLoginInDateTimeLocal == null;
        }

        private void UpdateRemoteData()
        {
            var lastLoginInDateTimeLocal = PlaySessionData.Instance.lastLoginInDateTimeLocal;
            if (!lastLoginInDateTimeLocal.HasValue)
                RetentionChanged = true;
            else
                RetentionChanged =
                    DateTime.Compare(_timeRepository.LocalNow().Date, lastLoginInDateTimeLocal.Value.Date) > 0;
            if (RetentionChanged) PlaySessionData.Instance.activeDays++;
            PlaySessionData.Instance.lastLoginInDateTimeLocal = _timeRepository.LocalNow();
            PlaySessionData.Instance.sessionId ??= 0;
            PlaySessionData.Instance.sessionId++;
            PlaySessionData.Instance.lastLoginInDateTimeLocal = _timeRepository.LocalNow();

            PlaySessionData.Instance.UpdateToServer();
        }

        private void OverwriteRemoteData()
        {
            // ModeTimes là index RAM LƯỜI — chỉ chứa mode nào ĐÃ được đọc/ghi trong phiên này;
            // chép nó lúc đầu phiên là seed server bằng sổ trắng (nửa còn lại của án 29→0).
            // Materialize mode SDK-own trước khi chép; mode riêng của game nếu vẫn thiếu thì từ
            // nay vô hại — đường đọc/ghi ở trên tự fallback + tự hàn theo reserve.
            _reserveRepository.GetModeTotalSec(PlayerSessionService.TOTAL_TIME_MODE);

            Dictionary<string, long> gameModeToTotalSec = new();
            foreach (var (key, basicPoolData) in _reserveRepository.ModeTimes)
                gameModeToTotalSec[key] = basicPoolData.Value;
            PlaySessionData.Instance.gameModeToTotalSec = gameModeToTotalSec;
            PlaySessionData.Instance.firstLogInMillis = _reserveRepository.FirstLogInMillis;
            PlaySessionData.Instance.activeDays = _reserveRepository.ActiveDays;
            PlaySessionData.Instance.sessionId = _reserveRepository.SessionId;
            PlaySessionData.Instance.lastLoginInDateTimeLocal = _reserveRepository.LastLoginInDateTimeLocal;
            RetentionChanged = _reserveRepository.RetentionChanged;
            PlaySessionData.Instance.UpdateToServer();
        }
    }
}