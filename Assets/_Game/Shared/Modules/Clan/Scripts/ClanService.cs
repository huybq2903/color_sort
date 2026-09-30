using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.ChatRoom.Runtime;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.Network;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.Common;
using Falcon.Shared.Lives;
using UnityEngine;

namespace Game.Shared.Clan
{
    /// <summary>Gộp 2 lớp static clan cũ: bọc CS/SC thành UniTask, giữ trạng thái, làm cầu EventBus.</summary>
    public class ClanService : IInitialize
    {
        private const int TIMEOUT = 10;

        // Ba khoá LeaderboardService đã chờ sẵn, xem LeaderboardService.cs:29-31
        public const string EVENT_JOIN_CLAN = "falcon.modules.clan.join_clan";
        public const string EVENT_LEAVE_CLAN = "falcon.modules.clan.leave_clan";
        public const string EVENT_GET_USER_CLAN = "falcon.modules.clan.get_user_clan";

        // Các khoá cross-module khác manager cũ đã đăng ký, Leaderboard/Lives/Profile đang emit
        private const string EVENT_OPEN_CLAN_INFO_POPUP = "falcon.modules.clan.open_clan_info_popup";
        private const string EVENT_GET_TOTAL_HELP_RESOURCES = "falcon.modules.clan.get_total_help_resources";
        private const string EVENT_OPEN_HELP_RESOURCES_POPUP = "falcon.modules.clan.open_help_resources_popup";
        private const string EVENT_GET_LOGO_TRANS = "falcon.modules.clan.get_logo_trans";
        // Key cua ProfileManager.EVENT_SAVED, ban thang khong ref sang asmdef Profile
        private const string EVENT_PROFILE_SAVED = "falcon.modules.ui.edit_profile_click_save";

        #region State (thay field static của manager cũ)

        public int MyClanCode { get; private set; }
        public bool Connected { get; private set; }
        public int LevelUnlock { get; private set; } = -1;
        public bool Unlocked => LevelUnlock >= 0 && CurrentLevel >= LevelUnlock;
        public int MaxLevelLimit { get; private set; } = -1;
        public int ClanCreateFee { get; private set; } = -1;
        public long CooldownPerRequest { get; private set; } = -1;
        public DateTime TimeCanCreateRequest { get; internal set; } = DateTime.MinValue;
        public int CoinReceivePerHelp { get; private set; } = -1;

        private int _totalHelpResource;

        public int TotalHelpResource
        {
            get => _totalHelpResource;
            set => _totalHelpResource = value;
        }

        private static int CurrentLevel => GameRequest<int>.Request(GameKeys.GET_LEVEL);

        #endregion

        #region Events (thay 12 UnityAction static của lớp event cũ)

        public event Action OnConnectionChanged;
        public event Action OnClanChanged;
        public event Action OnChangeUserData;
        public event Action<SCClanInfo> OnClanInfo;
        public event Action<SCSetMemberRoleInClan> OnSetMemberRole;
        public event Action<SCGetHelpResourceData> OnGetHelpResourceData;
        public event Action<SCGetHelpConfig> OnGetHelpConfig;

        #endregion

        #region Danh tính, level, unlock (Task 6 - trước đi qua interface custom cũ)

        public int YourPlayerCode() => AccountManager.Instance.Code;

        #endregion

        #region Logo (Task 6 - trỏ sang ClanLogoDatabase, asset đổ dữ liệu ở Task 10)

        public Sprite GetLogoById(int id) => ClanLogoDatabase.Instance?.GetById(id);

        #endregion

        #region Tài nguyên vàng (Task 6 - gọi thẳng ResourceCollector, theo phán quyết chủ repo)

        private const string GOLD = "gold";

        /// <summary>Chỉ trừ khi vẫn còn đủ tiền lúc gọi (double-check như bản gốc, chống lệch số dư giữa lúc bấm và lúc server xác nhận).</summary>
        public void PayClanCreationFee()
        {
            if (!CanCreateClan()) return;
            ResourceCollector.Instance.ResourceRemove(GOLD, ClanCreateFee, "create_clan");
            ResourceCollector.Instance.SaveAndUpdateServerOfResourceData(GOLD);
        }

        public bool CanCreateClan()
        {
            int gold = (int)ResourceCollector.Instance.GetResourceValueInCollector(GOLD);
            if (gold < ClanCreateFee) Toast("Not enough gold!");
            return gold >= ClanCreateFee;
        }

        public void GetRewardAfterHelpSuccess()
        {
            ResourceCollector.Instance.ResourceAdd(GOLD, CoinReceivePerHelp, "clan_help_reward");
            ResourceCollector.Instance.SaveAndUpdateServerOfResourceData(GOLD);
        }

        #endregion

        #region Help Resource / lives (Task 6 - gọi thẳng WrapperLives, chủ sở hữu mốc hồi tim + clamp)

        public bool CanAddHelpResource()
        {
            bool isMax = Center.Get<WrapperLives>()?.IsMax ?? false;
            if (isMax) Toast("Your lives are full!");
            return !isMax;
        }

        public void AddHelpResourceSuccess() => Center.Get<WrapperLives>()?.Add(1);

        #endregion

        /// <summary>Kiểm tra tên clan hợp lệ, dùng cho cả tạo mới và sửa.</summary>
        public bool ClanInfoAvailable(ClanEditingData clanInfo)
        {
            string name = clanInfo.name;
            if (!Regex.IsMatch(name, "^.{4,21}$"))
            {
                Toast("The name must have a length of 4-21 characters");
                return false;
            }
            if (!Regex.IsMatch(name, "^[a-zA-Z0-9_ ]*$"))
            {
                Toast("The name cannot contain special characters");
                return false;
            }
            return true;
        }

        #region Avatar, name style (Task 6 - qua EventBus, module Profile chưa nghe nên hiện mặc định)

        public const string EVENT_BIND_AVATAR = "falcon.modules.clan.bind_avatar";
        public const string EVENT_BIND_NAME_STYLE = "falcon.modules.clan.bind_name_style";

        public void OnSetAvatarUI(GameObject avatarUIObject, ClanMemberInfo data) =>
            GameEvent<(GameObject, int)>.Emit(EVENT_BIND_AVATAR, (avatarUIObject, data.code));

        public void OnSetNameStyleUI(GameObject normalObj, GameObject styleObj, GameObject nameStyleUIObject, ClanMemberInfo data) =>
            GameEvent<(GameObject, GameObject, GameObject, int)>.Emit(EVENT_BIND_NAME_STYLE, (normalObj, styleObj, nameStyleUIObject, data.code));

        #endregion

        #region Popup (Task 6 - trước đi qua interface custom cũ, giờ gọi thẳng UIWrapper)

        public const string EVENT_SHOW_PLAYER_INFO = "falcon.modules.clan.show_player_info";

        /// <summary>Mở popup profile người khác; Profile tự đọc code qua event, TODO: chưa ai nghe.</summary>
        public void ShowPlayerInfo(int playerCode) =>
            UIWrapper.OpenPopup("PopupProfile", popup => GameEvent<int>.Emit(EVENT_SHOW_PLAYER_INFO, playerCode));

        public void ShowHelpResourcePopup() =>
            UIWrapper.OpenPopup("UIPopupHelpResource", popup => popup.GetComponent<UIPopupHelpResource>().Bind());

        #endregion

        #region Toast (Task 6 - trước đi qua interface custom cũ, giờ qua Toast() có sẵn)

        public void ShowToast(string toast) => Toast(toast);
        public void ShowToastSuccess() => Toast("clan_toast_success");
        public void ShowToastFailed() => Toast("clan_toast_failed");
        public void ShowToastTimeout() => Toast("clan_toast_timeout");
        public void ShowToastWaitAMomentAfterOnClick() => Toast("clan_toast_wait_a_moment");

        #endregion

        /// <summary>Trả DateTime.Now, giữ lại để không phải sửa các nơi đang gọi kiểu này.</summary>
        public DateTime GetDateTimeNow() => DateTime.Now;

        /// <summary>Mở popup xem info 1 clan theo code, tự gửi CSClanInfo để đổ dữ liệu.</summary>
        public void ShowClanInfoPopup(int clan_code, bool fromClanPanel = false)
        {
            UIWrapper.OpenPopup("UIPopupClanInfo", popup =>
            {
                var ui = popup.GetComponent<UIPopupClanInfo>();
                ui.CloseOnJoinSuccess = fromClanPanel;
                new CSClanInfo(clan_code).AddSCListenerExt<SCClanInfo>((csMessage, scMessage, timeout, success) =>
                {
                    if (success && scMessage?.clan_info != null)
                    {
                        scMessage.clan_info.editable = scMessage.editable;
                        ui.Bind(scMessage.clan_info);
                    }
                    else if (timeout) ShowToastTimeout();
                }, int.MaxValue).Send();
            });
        }

        #region Boot

        public void OnInitialize()
        {
            GameEvent<(int, Action<int, string, Sprite>)>.Register(EVENT_GET_USER_CLAN, OnAskMyClan, null);

            // Giữ nguyên các key cross-module manager cũ đã đăng ký (Leaderboard/Lives dùng)
            GameEvent<int>.Register(EVENT_OPEN_CLAN_INFO_POPUP, code => ShowClanInfoPopup(code), null);
            GameEvent<Action<int>>.Register(EVENT_GET_TOTAL_HELP_RESOURCES, action => action?.Invoke(TotalHelpResource), null);
            GameEvent.Register(EVENT_OPEN_HELP_RESOURCES_POPUP, ShowHelpResourcePopup, null);
            GameEvent<(int, Action<RectTransform>)>.Register(EVENT_GET_LOGO_TRANS, logoIdAndAction =>
            {
                GameObject pref = Resources.Load<GameObject>("ClanLogo_ForUser");
                Transform trans = GameObject.Instantiate(pref).transform;
                trans.GetComponent<ClanLogoUI>().Init(logoIdAndAction.Item1);
                logoIdAndAction.Item2.Invoke(trans as RectTransform);
            }, null);

            // Ke tuc ClanPanelManager_ByUser.OnChangeProfile cu, bao popup/chat clan reload sau khi sua ten/avatar
            GameEvent.Register(EVENT_PROFILE_SAVED, NotifyChangeUserData, null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RequestConfig()
        {
            new CSClanConfig().Send();
            new CSGetHelpConfig().Send();
        }

        private void OnAskMyClan((int code, Action<int, string, Sprite> reply) req)
        {
            if (req.code == AccountManager.Instance.Code) req.reply?.Invoke(MyClanCode, null, null);
        }

        #endregion

        #region SC* gọi vào đây khi có dữ liệu

        internal void SetConnected(bool value)
        {
            if (Connected == value) return;
            Connected = value;
            OnConnectionChanged?.Invoke();
        }

        /// <summary>SCClanConfig gọi vào đây; myClanCode là đồng bộ ban đầu nên không bắn OnClanChanged.</summary>
        internal void SetConfig(int myClanCode, int levelUnlock, int maxLevelLimit, int clanCreateFee, bool isShowStatusOnline)
        {
            MyClanCode = myClanCode;
            LevelUnlock = levelUnlock;
            MaxLevelLimit = maxLevelLimit;
            ClanCreateFee = clanCreateFee;

            // Giống LeaderboardService: config về cũng là lúc Ready có thể đổi
            OnConnectionChanged?.Invoke();
        }

        internal void SetHelpConfig(long cooldownPerRequest, int coinReceivePerHelp, long nextRequestTimeLeftMs, int totalHelpResource)
        {
            CooldownPerRequest = cooldownPerRequest;
            TimeCanCreateRequest = DateTime.Now.AddMilliseconds(nextRequestTimeLeftMs);
            CoinReceivePerHelp = coinReceivePerHelp;
            TotalHelpResource = totalHelpResource;
        }

        /// <summary>Đổi clan hiện tại và báo cho Leaderboard biết. Dùng cho leave (join cần fetch tên/logo trước).</summary>
        internal void SetMyClan(int clanCode, string clanName, Sprite logo)
        {
            MyClanCode = clanCode;
            if (clanCode > 0)
                GameEvent<(int, string, Sprite)>.Emit(EVENT_JOIN_CLAN, (clanCode, clanName, logo));
            else
                GameEvent.Emit(EVENT_LEAVE_CLAN);
            OnClanChanged?.Invoke();
        }

        /// <summary>SCJoinClan chỉ có code; cập nhật code ngay rồi fetch tên/logo để báo module khác (giống EmitJoinClanAction cũ).</summary>
        internal void OnJoinClanReceived(int clanCode)
        {
            MyClanCode = clanCode;
            OnClanChanged?.Invoke();
            GetClanInfoOfUser(YourPlayerCode(), data =>
                GameEvent<(int, string, Sprite)>.Emit(EVENT_JOIN_CLAN, (clanCode, data?.name, GetLogoById(data?.avatar_id ?? 0))));
        }

        internal void OnLeaveClanReceived() => SetMyClan(0, null, null);

        internal void RaiseClanInfo(SCClanInfo sc) => OnClanInfo?.Invoke(sc);
        internal void RaiseSetMemberRole(SCSetMemberRoleInClan sc) => OnSetMemberRole?.Invoke(sc);
        internal void RaiseGetHelpResourceData(SCGetHelpResourceData sc) => OnGetHelpResourceData?.Invoke(sc);
        internal void RaiseGetHelpConfig(SCGetHelpConfig sc) => OnGetHelpConfig?.Invoke(sc);

        /// <summary>ClanPanelManager_ByUser gọi khi user đổi tên/profile xong.</summary>
        public void NotifyChangeUserData() => OnChangeUserData?.Invoke();

        #endregion

        public void Toast(string localizeKey) => GameEvent<string>.Emit(GameKeys.TOAST_OPEN_LOCALIZE, localizeKey);

        /// <summary>Lấy clan của 1 user qua CSClanOfUsers; dùng nội bộ để lấy tên+logo cho SetMyClan/EventBus.</summary>
        private void GetClanInfoOfUser(int userCode, Action<ClanDataShort> onSuccess)
        {
            new CSClanOfUsers(userCode).AddSCListener<SCClanOfUsers>((message, timeout, success) =>
            {
                if (success && message?.clanInfos != null && message.clanInfos.Count > 0)
                    onSuccess?.Invoke(message.clanInfos[0]);
            }, TIMEOUT).Send();
        }

        #region 5 hàm hỏi đáp mới (UniTask), dùng cho Task 6

        public async UniTask<SCClanInfo> FetchClanInfo(int clanCode, CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource<SCClanInfo>();
            new CSClanInfo(clanCode)
                .AddSCListener<SCClanInfo>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null) Debug.LogWarning("[Clan] lấy thông tin clan thất bại.");
            return sc;
        }

        public async UniTask<SCRandomListClan> FetchRandomList(CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource<SCRandomListClan>();
            new CSRandomListClan()
                .AddSCListener<SCRandomListClan>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null) Debug.LogWarning("[Clan] lấy danh sách clan thất bại.");
            return sc;
        }

        public async UniTask<SCSearchClan> Search(string keyword, CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource<SCSearchClan>();
            new CSSearchClan { clan_name = keyword }
                .AddSCListener<SCSearchClan>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null) Debug.LogWarning("[Clan] tìm clan thất bại.");
            return sc;
        }

        // CSJoinClanReq trả lời bằng SCResponse_Clan; SCJoinClan là bản tin riêng khi thực sự được nhận vào clan (xem OnJoinClanReceived)
        public async UniTask<SCResponse_Clan> Join(int clanCode, CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource<SCResponse_Clan>();
            new CSJoinClanReq(clanCode)
                .AddSCListener<SCResponse_Clan>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null) Debug.LogWarning("[Clan] tham gia clan thất bại.");
            return sc;
        }

        public async UniTask<SCLeaveClan> Leave(CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource<SCLeaveClan>();
            new CSLeaveClan()
                .AddSCListener<SCLeaveClan>((msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null) Debug.LogWarning("[Clan] rời clan thất bại.");
            return sc;
        }

        #endregion
    }

    /// <summary>Báo trạng thái kết nối cho ClanService. FNetManager tự quét và tạo instance qua reflection.</summary>
    public class ClanSessionListener : ISessionListener
    {
        private static ClanService Service => Center.GetOrCreate<ClanService>();

        public void OnFirstSession() => Service.SetConnected(true);
        public void OnSessionReset() => Service.SetConnected(true);
        public void OnChannelDisconnected(FChannel fChannel) => Service.SetConnected(false);
    }
}
