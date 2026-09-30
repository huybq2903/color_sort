/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-06
 */

namespace Falcon.Shared.Common
{
    /// Key EventBus dùng chung từ 2 assembly trở lên. Key nội bộ 1 module thì để trong module đó.
    public static class GameKeys
    {
        // Scene flow
        public const string PLAY_LEVEL = "play_level";
        public const string BACK_TO_HOME = "back_to_home";
        public const string NEXT_ON_WIN = "next_on_win";

        // Vòng đời level
        public const string LOAD_SCENE_LEVEL_COMPLETE = "falcon.game.load_scene_level_complete";
        public const string SETUP_LEVEL_COMPLETE = "falcon.game.setup_level_complete";
        public const string LEVEL_RUNTIME = "falcon.game.level_runtime";
        public const string CLEAR_LEVEL_RUNTIME = "falcon.game.clear_level_runtime";

        // Thắng / thua
        public const string WIN = "falcon.game.win";
        public const string LOSE = "falcon.game.lose";
        public const string TEMP_LOSE = "falcon.game.temp_lose";
        public const string REVIVE = "falcon.game.revive";
        public const string DO_WIN = "falcon.game.do_win";
        public const string DO_TEMP_LOSE = "falcon.game.do_temp_lose";
        public const string LOG_SCORE = "falcon.game.log_score";
        public const string ON_WIN_LEVEL = "falcon.game.on_win.level";
        public const string WIN_FLOW = "falcon.ingame.win.flow";
        public const string TEMP_LOSE_FLOW = "falcon.ingame.temp_lose.flow";

        // Flow popup mỗi lần vào Home. Order quy ước xem HomeFlowRunner.ORDER_*.
        public const string HOME_FLOW = "falcon.outgame.home.flow";

        // Booster
        public const string USE_BOOSTER = "falcon.game.use_booster";
        public const string USE_BOOSTER_BY_REVIVE = "falcon.game.use_booster.by_revive";
        public const string AFTER_BOOSTER_USED = "falcon.game.after_booster_used";

        // Request config / data
        public const string GET_REMOTE_CONFIG = "get_remote_config";
        public const string GET_LEVEL = "falcon.modules.core.gamedata.get_level";
        public const string GET_LEVEL_DIFFICULTY = "get_level_difficulty";

        // Lives
        public const string CAN_USE_LIVE = "can_use_live";
        public const string USE_LIVE = "use_live";
        public const string CLEAR_CACHE_LIVE = "clear_cache_live";
        public const string RELEASE_CACHE_LIVE = "release_cache_live";

        // UI (key popup open/close dùng Falcon.Modules.Core.UI.Runtime.Const, không khai lại ở đây)
        public const string TOAST_OPEN = "falcon.modules.ui.toast_open";
        public const string TOAST_OPEN_LOCALIZE = "falcon.modules.ui.toast_open.localize";
        public const string SET_ALPHA_HUD = "falcon.game.set_alpha_hud";
        public const string GOLD_UPDATE = "falcon.ui.gold_update";
        public const string PURCHASE_CLOSE_SUCCESS = "falcon.modules.ui.purchase.close_success";
        public const string REWARD_ENTRY_SPAWN = "falcon.modules.ui.reward_entry_spawn";
        public const string REWARD_ENTRY_SPAWN_GOLD = "falcon.modules.ui.reward_entry_spawn_gold";
        public const string FORMAT_QUANTITY_REWARD = "falcon.modules.format_quantity_reward";

        // Audio / settings
        public const string PLAY_SFX = "falcon.modules.audio.play_sfx";
        public const string PLAY_MUSIC = "falcon.modules.audio.play_music";
        public const string STOP_MUSIC = "falcon.modules.audio.stop_music";
        public const string SETTINGS_MEDIA_SAVE = "falcon.modules.ui.settings_media_save";
    }
}
