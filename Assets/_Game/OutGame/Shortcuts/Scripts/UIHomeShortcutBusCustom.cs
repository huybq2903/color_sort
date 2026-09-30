/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-24
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.UI.Menu.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Falcon.OutGame.ShortcutHome
{
    public class UIHomeShortcutBusCustom : UIHomeShortcutsReceiveBus
    {
        // MenuConst chưa có key expand, khai báo tại chỗ để khỏi đụng vào module Falcon.
        private const string EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_EXPAND = "falcon.modules.ui.home_shortcut_add_expand";

        // Layout expand chỉ chứa được một cái.
        private const int MaxExpand = 1;

        private readonly Dictionary<string, GameObject> _shortcutsLeft = new();
        private readonly Dictionary<string, GameObject> _shortcutsRight = new();
        private readonly Dictionary<string, GameObject> _shortcutsExpand = new();
        private readonly List<string> _lsHide = new();

        private int _maxOneSide;
        private List<string> _priorities;
        private CanvasGroup _canvasGroup;

        protected override void OnEnable()
        {
            base.OnEnable();
            GameEvent<(AssetReference asset, string id)>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_LEFT, AddShortcutLeft, this);
            GameEvent<(AssetReference asset, string id)>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_RIGHT, AddShortcutRight, this);
            GameEvent<string>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_LEFT, AddShortcutLeft, this);
            GameEvent<string>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_RIGHT, AddShortcutRight, this);
            GameEvent<(AssetReference asset, string id)>.Register(EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_EXPAND, AddShortcutExpand, this);
            GameEvent<string>.Register(EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_EXPAND, AddShortcutExpand, this);
            GameEvent<string>.Register(MenuConst.EVENT_UI_HOME_SHORTCUT_REMOVE, HideShortcut, this);
            _maxOneSide = HomeShortcutManager.quantityMaxOneSide;
            _priorities = HomeShortcutManager.priorities ?? new List<string>();
        }

        private async void Start()
        {
            try
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                _canvasGroup.alpha = 0f;
                foreach (var (id, asset) in HomeShortcutManager.dictAssetLeft.ToDictionary(d => d.Key, d => d.Value))
                {
                    await AddShortcut((asset, id), _shortcutsLeft, layoutLeft);
                }

                foreach (var (id, asset) in HomeShortcutManager.dictAssetRight.ToDictionary(d => d.Key, d => d.Value))
                {
                    await AddShortcut((asset, id), _shortcutsRight, layoutRight);
                }

                foreach (var (id, asset) in HomeShortcutManager.dictAssetExpand.ToDictionary(d => d.Key, d => d.Value))
                {
                    await AddShortcut((asset, id), _shortcutsExpand, layoutExpand);
                }

                SetupShortcut(_shortcutsLeft, layoutLeft);
                SetupShortcut(_shortcutsRight, layoutRight);
                SetupShortcut(_shortcutsExpand, layoutExpand, MaxExpand);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
            finally
            {
                _canvasGroup.alpha = 1f;
            }
        }

        private async void AddShortcutLeft((AssetReference asset, string id) shortcut)
        {
            await AddShortcut(shortcut, _shortcutsLeft, layoutLeft);
            SetupShortcut(_shortcutsLeft, layoutLeft);
        }

        private async void AddShortcutRight((AssetReference asset, string id) shortcut)
        {
            await AddShortcut(shortcut, _shortcutsRight, layoutRight, false);
            SetupShortcut(_shortcutsRight, layoutRight);
        }

        private async void AddShortcutLeft(string nameAsset)
        {
            var shortcut = (HomeShortcutManager.dictAssetLeft[nameAsset], nameAsset);
            await AddShortcut(shortcut, _shortcutsLeft, layoutLeft);
            SetupShortcut(_shortcutsLeft, layoutLeft);
        }

        private async void AddShortcutRight(string nameAsset)
        {
            var shortcut = (HomeShortcutManager.dictAssetRight[nameAsset], nameAsset);
            await AddShortcut(shortcut, _shortcutsRight, layoutRight, false);
            SetupShortcut(_shortcutsRight, layoutRight);
        }

        private async void AddShortcutExpand((AssetReference asset, string id) shortcut)
        {
            await AddShortcut(shortcut, _shortcutsExpand, layoutExpand);
            SetupShortcut(_shortcutsExpand, layoutExpand, MaxExpand);
        }

        private async void AddShortcutExpand(string nameAsset)
        {
            var shortcut = (HomeShortcutManager.dictAssetExpand[nameAsset], nameAsset);
            await AddShortcut(shortcut, _shortcutsExpand, layoutExpand);
            SetupShortcut(_shortcutsExpand, layoutExpand, MaxExpand);
        }

        private async Task AddShortcut((AssetReference asset, string id) shortcut,
            Dictionary<string, GameObject> dict, Transform parent, bool isLeft = true)
        {
            _lsHide.Remove(shortcut.id);
            if (dict.TryGetValue(shortcut.id, out var go))
            {
                go?.SetActive(false);
            }
            else
            {
                dict[shortcut.id] = null; //Để đánh dấu k bị duplicate shortcut
                var handleIns = shortcut.asset.InstantiateAsync(parent);
                await handleIns.Task;
                // await Task.Yield();

                if (IsCanceled()) return;
                var goShortcut = handleIns.Result;
                if (!goShortcut) return;

                goShortcut.SendMessage("Setup", isLeft, SendMessageOptions.DontRequireReceiver);
                goShortcut.SetActive(false);

#if UNITY_EDITOR
                goShortcut.name = $"[Shortcut]{shortcut.id}";
#endif
                dict[shortcut.id] = goShortcut;
            }
        }

        private void SetupShortcut(Dictionary<string, GameObject> dict, Transform parent, int maxActive = -1)
        {
            if (maxActive < 0) maxActive = _maxOneSide;

            // Build ordered list: priorities first (in defined order), then the rest (unknown IDs)
            var ordered = new List<GameObject>(dict.Count);
            var prioritized = new HashSet<string>();

            foreach (var id in _priorities)
            {
                if (dict.TryGetValue(id, out var go) && go && !_lsHide.Contains(id))
                {
                    ordered.Add(go);
                    prioritized.Add(id);
                }
            }

            foreach (var (key, go) in dict)
            {
                if (prioritized.Contains(key)) continue;
                if (go && !_lsHide.Contains(key))
                {
                    ordered.Add(go);
                }
            }

            // Reserve slots for shortcuts not managed by this dict (added by other systems)
            var ourObjects = new HashSet<int>();
            foreach (var go in ordered)
            {
                if (go)
                {
                    ourObjects.Add(go.GetInstanceID());
                }
            }

            var externalActiveCount = 0;
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i).gameObject;
                if (!child || !child.activeSelf) continue;
                if (!ourObjects.Contains(child.GetInstanceID()))
                {
                    externalActiveCount++;
                }
            }

            var remainingSlotsForUs = maxActive - externalActiveCount;
            if (remainingSlotsForUs < 0) remainingSlotsForUs = 0;

            // Activate our items up to remaining slots in defined order; hide the rest
            var ourVisible = 0;
            foreach (var go in ordered)
            {
                if (!go) continue;

                if (go.transform.parent != parent)
                {
                    go.transform.SetParent(parent);
                }
                go.transform.SetAsLastSibling();

                if (ourVisible < remainingSlotsForUs)
                {
                    go.SetActive(true);
                    ourVisible++;
                }
                else
                {
                    go.SetActive(false);
                }
            }
        }

        private void HideShortcut(string shortcutId)
        {
            _lsHide.Add(shortcutId);

            if (_shortcutsLeft.TryGetValue(shortcutId, out var go1) && go1)
            {
                go1.transform.SetAsLastSibling();
                go1.SetActive(false);
            }

            if (_shortcutsRight.TryGetValue(shortcutId, out var go2) && go2)
            {
                go2.transform.SetAsLastSibling();
                go2.SetActive(false);
            }

            if (_shortcutsExpand.TryGetValue(shortcutId, out var go3) && go3)
            {
                go3.transform.SetAsLastSibling();
                go3.SetActive(false);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameEvent<(AssetReference asset, string id)>.Unregister(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_LEFT, AddShortcutLeft, this);
            GameEvent<(AssetReference asset, string id)>.Unregister(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_RIGHT, AddShortcutRight, this);
            // Thiếu 2 dòng overload string là mỗi lần bật lại Home lại đăng ký chồng thêm một lần.
            GameEvent<string>.Unregister(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_LEFT, AddShortcutLeft, this);
            GameEvent<string>.Unregister(MenuConst.EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_RIGHT, AddShortcutRight, this);
            GameEvent<(AssetReference asset, string id)>.Unregister(EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_EXPAND, AddShortcutExpand, this);
            GameEvent<string>.Unregister(EVENT_UI_HOME_SHORTCUT_ADD_SHORTCUT_EXPAND, AddShortcutExpand, this);
            GameEvent<string>.Unregister(MenuConst.EVENT_UI_HOME_SHORTCUT_REMOVE, HideShortcut, this);
        }

        private bool IsCanceled() => !this || !gameObject;
    }
}
