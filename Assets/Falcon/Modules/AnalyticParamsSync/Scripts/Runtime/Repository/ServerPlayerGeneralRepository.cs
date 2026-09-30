/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-21
 */
using System;
using System.Threading;
using System.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Level.Core;

// ReSharper disable once CheckNamespace
namespace Falcon.Modules.AnalyticParamsSync
{
    [Primary]
    public class ServerPlayerGeneralRepository : IFPlayerGeneralRepository, IInit
    {
        private readonly ReservePlayerGeneralRepository _reserveRepository;

        public ServerPlayerGeneralRepository(ReservePlayerGeneralRepository reserveRepository)
        {
            _reserveRepository = reserveRepository;
        }

        public string AccountID => PlayerGeneralData.Instance?.bigDataAccountId ?? _reserveRepository.AccountID;

        public int MaxPassedLevel
        {
            // Max với sổ reserve chứ không trả trần level-1: LevelData.Instance.level là CON TRỎ
            // màn đang chơi — game cho chọn/replay màn cũ là con trỏ tụt, maxPassedLevel trên MỌI
            // log tụt theo (đúng án 🐞2 loader audit 12/08; bản cũ còn nuốt luôn setter nên fix
            // Math.Max ở LevelLogDecorService vô hiệu khi repo này bật). Counter đơn điệu: sổ nào
            // lớn hơn là sổ đúng — cùng triết lý ServerPlayerSessionRepository.
            get => Math.Max(LevelData.Instance.level - 1, _reserveRepository.MaxPassedLevel);
            set
            {
                // Ghi vào sổ reserve (max — không cho tụt) thay vì nuốt: decor set khi pass thật,
                // nuốt nó là mất đường ghi duy nhất không phụ thuộc con trỏ level của game.
                _reserveRepository.MaxPassedLevel = Math.Max(_reserveRepository.MaxPassedLevel, value);
            }
        }

        public string InstallVersion => PlayerGeneralData.Instance?.installVersion ?? _reserveRepository.InstallVersion;

        public string AdvertisingID => AccountManager.Instance?.ClientData?.accountInfo?.advertising_id ??
                                       _reserveRepository.AdvertisingID;

        public async Task Init(CancellationToken cancellationToken = default)
        {
            if (!RemoteUncertain()) return;
            if(!await AccountLoginListener.WaitLogin()) return;
            if (!RemoteUncertain()) return;
            OverwriteRemoteData();
        }

        private static bool RemoteUncertain()
        {
            var playerGeneralData = PlayerGeneralData.Instance;
            return playerGeneralData.bigDataAccountId == null
                   || playerGeneralData.installVersion == null;
        }

        private void OverwriteRemoteData()
        {
            PlayerGeneralData.Instance.bigDataAccountId ??= _reserveRepository.AccountID;
            PlayerGeneralData.Instance.installVersion ??= _reserveRepository.InstallVersion;
            PlayerGeneralData.Instance.UpdateToServer();
        }
    }
}