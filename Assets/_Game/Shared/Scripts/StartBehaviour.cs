/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-12
 */

using System;
using System.Collections.Generic;
using System.IO;
using BayatGames.SaveGamePro.Serialization;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.Devkit;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.GameData.Runtime;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Modules.InAppLog.Bigdata.Runtime;
using Falcon.Modules.InAppUpdate.Scripts.Runtime;
using Falcon.Modules.Level.Core;
using Falcon.Modules.Packs.Core.Runtime;
using Falcon.Modules.Shop.Runtime;
using Falcon.Modules.UI.Menu.Runtime;
using Falcon.Modules.UnityLocalization.Runtime;
using Falcon.OutGame.Core;
using Game.Shared.Clan;
using Game.Shared.Leaderboard;
using Game.Shared.Profile;
using Falcon.Shared.Audio;
using Falcon.Shared.BaseInGame;
using Falcon.Shared.BaseLevelEditor;
using Falcon.Shared.Common;
using Falcon.Shared.Common.Time;
using Falcon.Shared.LoadingTransition;
using Falcon.Shared.Mediation;
using Falcon.Shared.Lives;
using I2.Loc;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Falcon.Shared.Performance;
using LevelData = Falcon.Modules.Level.Core.LevelData;
using Md5Utils = Falcon.Shared.BaseInGame.Md5Utils;
using Falcon.Shared.BaseEvents;

public class StartBehaviour : MonoBehaviour
{
    // Chạy trước mọi Awake/Start nên chặn được log của script khởi tạo sớm.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void DisableDeveloperConsoleEarly()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.developerConsoleEnabled = false;
        Debug.unityLogger.logEnabled = true;
#else
        Debug.unityLogger.logEnabled = false;
#endif
        // MaxSdkUnityEditor chỉ compile khi UNITY_EDITOR hoặc platform không phải Android/iOS.
#if UNITY_EDITOR
        MaxSdkUnityEditor.SetExtraParameter("disable_all_logs", "true");
#endif
    }

    private void Start()
    {
        SetFrameRate();
        LoadSceneFromStart().Forget();
        InitCallback();
        InitModules();
        /*
        new FFilteredFunnelLog(
               "level_funnel_drop",
               $"start",
               0,
               1
           ).Send();*/
    }

    private static void SetFrameRate()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        FrameRateSetup.Apply();
    }

    private static async UniTaskVoid LoadSceneFromStart()
    {
        await WaitForServerLevelSync();
        LoadScene(TransitionType.ProgressBar);
    }

    private void InitCallback()
    {
        BigdataLogger.IsContainWhyInProductId = true;
        FormatHelper.FormatQuantityReward = FormatQuantityReward;
        GlobalButtonListener.Register(OnButtonReleased);
        GameRequest<(string name, int amount), string>.Register(GameKeys.FORMAT_QUANTITY_REWARD, FormatQuantityReward);
        GameRequest<string>.Register("current_tab_navigator", GetCurrentTabNavigator);
        GameRequest<int, int>.Register(GameKeys.GET_LEVEL_DIFFICULTY, GetLevelDifficulty);
        GameRequest<int, int>.Register("get_level_difficulty_event", GetLevelDifficultyEvent);
        GameRequest<int>.Register(GameKeys.GET_LEVEL_DIFFICULTY, GetLevelDifficulty);
        GameEvent.Register(GameKeys.PLAY_LEVEL, () => LoadGameScene(TransitionType.IrisWipe));
        GameEvent.Register(GameKeys.BACK_TO_HOME, () => LoadHomeScene(TransitionType.IrisWipe));
        GameEvent.Register(GameKeys.NEXT_ON_WIN, () => LoadScene(TransitionType.IrisWipe));
        GameEvent<int>.Register(GameKeys.ON_WIN_LEVEL, OnWinGame);
        GameEvent<string>.Register(GameKeys.TOAST_OPEN_LOCALIZE, OpenToastLocalize);
        Messenger<OnSetTempLevel>.Register(data => LevelData.Instance.level = data.level);
        ShopManager.dictKeyGroupTitle = new Dictionary<int, Func<string>>
        {
            { 1, () => LocalizationManager.GetTranslation("shop.special") },
            { 2, () => LocalizationManager.GetTranslation("shop.ads") },
            { 3, () => LocalizationManager.GetTranslation("shop.bundle") },
            { 4, () => LocalizationManager.GetTranslation("shop.currency") },
        };
    }

    private static WrapperLives _wrapperLives;

    private void InitModules()
    {
        try { InitService.Init(); } catch (Exception e) { Debug.LogException(e); }
        try { WrapperTime.Initialize(); } catch (Exception e) { Debug.LogException(e); }
        try { GameConfig.Init(); } catch (Exception e) { Debug.LogException(e); }
        try { SaveGameTypeManager.Initialize(); } catch (Exception e) { Debug.LogException(e); }
        try { FGameData.Initialize(); } catch (Exception e) { Debug.LogException(e); }
        try { GameDataRunner.SceneLoad(); } catch (Exception e) { Debug.LogException(e); }
        try { AccountManager.Instance.Init(); } catch (Exception e) { Debug.LogException(e); }
        try { MediationHelpers.Instance.Initialize(); } catch (Exception e) { Debug.LogException(e); }
        try { AudioManager.InitSettings(); } catch (Exception e) { Debug.LogException(e); }
        try { ProfileManager.Initialize(); } catch (Exception e) { Debug.LogException(e); }
        try { Center.GetOrCreate<WrapperGoldHome>(); } catch (Exception e) { Debug.LogException(e); }
        try { _wrapperLives = Center.GetOrCreate<WrapperLives>(); } catch (Exception e) { Debug.LogException(e); }
        try { Center.GetOrCreate<LeaderboardService>(); } catch (Exception e) { Debug.LogException(e); }
        try { Center.GetOrCreate<ClanService>(); } catch (Exception e) { Debug.LogException(e); }
    }

    private static void OnWinGame(int level)
    {
        new CSGetEventsByLevel { level = level + 1 }.Send();
    }

    private static void OnButtonReleased(Button obj)
    {
        HapticManager.LightFeedback();
        AudioManager.PlaySFX(SoundEnum.Click);
    }

    private static int GetLevelDifficulty(int level) => FLevelManager.Instance.GetLevelDifficulty(level);

    private static int GetLevelDifficulty() => GetLevelDifficulty(LevelData.Instance.level);

    private static int GetLevelDifficultyEvent(int level) => GetLevelDifficulty(level);

    private static int _homeTabIndex = -1;
    private static int HomeTabIndex
    {
        get
        {
            if (_homeTabIndex >= 0) return _homeTabIndex;
            var config = Resources.Load<MenuFeaturesConfig>("SO_UI_Menu_FeaturesConfig");
            for (var i = 0; i < config.maxNumberTab; i++)
            {
                if (!config.GetFeature(i).Contains("UIHome")) continue;
                _homeTabIndex = i;
                break;
            }
            return _homeTabIndex;
        }
    }

    private static string GetCurrentTabNavigator()
    {
        if (UIWrapper.Manager.stackPopups.Count > 0) return string.Empty;

        var indexTab = -1;
        GameEvent<Action<int>>.Emit(MenuConst.EVENT_MENU_NAVIGATOR_GET_CURRENT_INDEX_TAB, index => indexTab = index);
        return indexTab >= 0 && indexTab == HomeTabIndex ? "home" : string.Empty;
    }

    private static void OpenToastLocalize(string text) => GameEvent<string>.Emit(GameKeys.TOAST_OPEN, ULocManager.GetLocalizedString(text));

    private static string FormatQuantityReward((string name, int amount) reward) => FormatQuantityReward((reward.name, reward.amount, null));

    private static string FormatQuantityReward((string name, int amount, string data) reward)
    {
        return reward.name switch
        {
            "gold" => $"{reward.amount.FormatWithSpace()}",
            "unlimited_live" => $"{reward.amount.FormatSeconds()}",
            "battle_pass_reward" => string.Empty,
            _ => $"x{reward.amount}"
        };
    }

    private static void LoadGameScene(TransitionType transitionType)
    {
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            AudioManager.StopMusic();
            AudioManager.PlayMusic("BGM_Level");
        }
        DataTempExtensions<string>.Set("level_data", FLevelManager.Instance.GetLevelData(LevelData.Instance.level));
        DataTempExtensions<string>.Set("level_data_param", FLevelManager.Instance.GetLevelParam(LevelData.Instance.level));
        DataTempExtensions<LevelRuntime>.Remove(GameKeys.LEVEL_RUNTIME);
        LoadingSceneManager.Load("GameScene", transitionType);
    }

    private static void LoadHomeScene(TransitionType transitionType)
    {
        AudioManager.StopMusic();
        AudioManager.PlayMusic("BGM_Home");
        LoadingSceneManager.Load("HomeScene", transitionType);
    }

    private static async UniTask WaitForServerLevelSync()
    {
        await UniTask.Yield();
        await PopupInAppUpdate.ShowUpdatePopupIfNeededAsync();
        var loginResult = await UniTask.WhenAny(UniTask.WaitUntil(() => AccountManager.Instance.IsLogin), UniTask.WaitForSeconds(2f));
        if (loginResult == 1) return;
        var serverLevelFile = Application.persistentDataPath + "/falcon/server_levels/" + LevelData.Instance.level + ".data";
        await UniTask.WhenAny(UniTask.WaitUntil(() => File.Exists(serverLevelFile)), UniTask.WaitForSeconds(2f));
    }

    private static void LoadScene(TransitionType transitionType)
    {
        var levelData = FLevelManager.Instance.GetLevelData(LevelData.Instance.level);

        // Còn dở level cũ -> vào thẳng, không tốn stamina.
        if (LevelRuntime.HasLevelRuntime(Md5Utils.GetMd5First5Char(levelData), out var levelRuntime))
        {
            DataTempExtensions<LevelRuntime>.Set(GameKeys.LEVEL_RUNTIME, levelRuntime);
            LoadGameScene(transitionType);
            return;
        }

        if (LevelData.Instance.level < GameConfig.Get("level_unlock_home", 5) && GameRequest<bool>.Request(GameKeys.CAN_USE_LIVE))
            LoadGameScene(transitionType);
        else
            LoadHomeScene(transitionType);
    }
}