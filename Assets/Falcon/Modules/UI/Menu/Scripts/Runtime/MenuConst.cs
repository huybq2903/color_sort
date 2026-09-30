/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-2
*/

namespace Falcon.Modules.UI.Menu.Runtime
{
    public class MenuConst
    {
        public const string EVENT_MENU_GET_GAME_OBJECT = "falcon.modules.ui.menu_get_game_object";
        public const string EVENT_MENU_LOAD_FEATURES_COMPLETE = "falcon.modules.core.ui_menu_load_features_complete";

        public const string EVENT_MENU_NAVIGATOR_GET_GAME_OBJECT = "falcon.modules.ui.menu.navigator_get_game_object";
        public const string EVENT_MENU_NAVIGATOR_GO_TO_TAB = "falcon.modules.ui.menu.navigator_go_to_tab";
        public const string EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED = "falcon.modules.ui.menu.navigator_on_tab_changed";
        public const string EVENT_MENU_NAVIGATOR_GET_CURRENT_INDEX_TAB = "falcon.modules.ui.menu.navigator_get_current_index_tab";
       
        public const string EVENT_UI_HOME_LOAD_COMPLETE = "falcon.modules.core.ui_home_load_complete";
        public const string EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_LEFT = "falcon.modules.ui.home_shortcut_add_left";
        public const string EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_RIGHT = "falcon.modules.ui.home_shortcut_add_right";
        public const string EVENT_UI_HOME_SHORTCUT_REMOVE = "falcon.modules.ui.home_shortcut_remove";
        public const string EVENT_UI_HOME_ADD_SHORTCUT_EXPAND = "falcon.modules.ui.home_shortcut_add_expand";

        public const string TWEEN_TAB_ANIMATOR_MENU_NAVIGATOR = "tween_tab_animator_menu_navigator";
    }
}
