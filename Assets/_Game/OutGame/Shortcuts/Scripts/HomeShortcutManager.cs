/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-29
 */

using System.Collections.Generic;
using Falcon.Helpers.EventBus;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Falcon.Modules.UI.Menu.Runtime;

namespace Falcon.OutGame.ShortcutHome
{
    public static class HomeShortcutManager
    {
        private const string EVENT_UI_HOME_SHORTCUT_REMOVE_IN_NEXT = "falcon.modules.ui.home_shortcut_remove_in_next";

        // MenuConst chưa có key expand, khai báo tại chỗ để khỏi đụng vào module Falcon.
        private const string EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_EXPAND = "falcon.modules.ui.home_shortcut_add_expand";

        internal static Dictionary<string, AssetReference> dictAssetLeft;
        internal static Dictionary<string, AssetReference> dictAssetRight;
        internal static Dictionary<string, AssetReference> dictAssetExpand;
        internal static int quantityMaxOneSide;
        internal static List<string> priorities;

        [RuntimeInitializeOnLoadMethod]
        private static async void Initialize()
        {
            dictAssetLeft = new Dictionary<string, AssetReference>();
            dictAssetRight = new Dictionary<string, AssetReference>();
            dictAssetExpand = new Dictionary<string, AssetReference>();
            GameEvent<(AssetReference asset, string id)>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_LEFT, AddShortcutLeft);
            GameEvent<(AssetReference asset, string id)>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_RIGHT, AddShortcutRight);
            GameEvent<(AssetReference asset, string id)>.Register(EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_EXPAND, AddShortcutExpand);
            GameEvent<string>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_LEFT, AddShortcutLeft);
            GameEvent<string>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_RIGHT, AddShortcutRight);
            GameEvent<string>.Register(EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_EXPAND, AddShortcutExpand);
            GameEvent<string>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_REMOVE, HideShortcut);
            GameEvent<string>.Register(EVENT_UI_HOME_SHORTCUT_REMOVE_IN_NEXT, HideShortcut);
            var handle = Resources.LoadAsync<SOShortcutConfig>("SO_UI_Menu_Home_ShortcutsConfig");
            await handle;
            var so = handle.asset as SOShortcutConfig;
            if (so)
            {
                quantityMaxOneSide = so.quantityMaxOneSide;
                priorities = so.priorities;
            }
        }

        private static void HideShortcut(string idShortcut)
        {
            dictAssetLeft.Remove(idShortcut);
            dictAssetRight.Remove(idShortcut);
            dictAssetExpand.Remove(idShortcut);
        }

        private static void AddShortcutLeft((AssetReference asset, string id) shortcut)
        {
            dictAssetLeft[shortcut.id] = shortcut.asset;
        }

        private static void AddShortcutRight((AssetReference asset, string id) shortcut)
        {
            dictAssetRight[shortcut.id] = shortcut.asset;
        }

        private static void AddShortcutRight(string nameAsset)
        {
            Debug.Log($"AddShortcutRight: {nameAsset}");
            dictAssetRight[nameAsset] = new AssetReference(nameAsset);
        }

        private static void AddShortcutLeft(string nameAsset)
        {
            dictAssetLeft[nameAsset] = new AssetReference(nameAsset);
        }

        private static void AddShortcutExpand((AssetReference asset, string id) shortcut)
        {
            dictAssetExpand[shortcut.id] = shortcut.asset;
        }

        private static void AddShortcutExpand(string nameAsset)
        {
            dictAssetExpand[nameAsset] = new AssetReference(nameAsset);
        }
    }
}