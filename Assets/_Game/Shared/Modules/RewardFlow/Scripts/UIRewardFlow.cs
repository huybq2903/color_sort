/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-06
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Falcon.Helpers.EventBus;
using Falcon.Shared.PoolManager;
using UnityEngine;
using Falcon.Shared.Common;

namespace Falcon.Shared.RewardFlow
{
    public class UIRewardFlow : MonoBehaviour
    {
        private const string EVENT_ENTRY_CUSTOM = "falcon.modules.ui.reward_entry_custom";
        private const string EVENT_ENTRY_LIST = "falcon.modules.ui.reward_entry_list";
        private const string EVENT_ENTRY_CHEST = "falcon.modules.ui.reward_entry_chest";
        private const string EVENT_ENTRY_FLOATING = "falcon.modules.ui.reward_entry_floating";
        private const string EVENT_CALLBACK_CLOSE = "falcon.modules.ui.reward_callback_close";
        private const string EVENT_FLOW_COMPLETE = "falcon.modules.ui.reward_flow_complete";

        public UIRewardEntryFloating entryFloatingPref;
        public UIRewardEntrySpawn entrySpawnPref;
        public UIRewardEntrySpawnGold entrySpawnGoldPref;
        public UIRewardEntryCustom entryCustomPref;
        public UIRewardEntryList entryListPref;
        public UIRewardEntryChest entryChestPref;

        private FObjectPool<UIRewardEntryFloating> _poolFloating;
        private FObjectPool<UIRewardEntrySpawn> _poolSpawn;
        private FObjectPool<UIRewardEntrySpawnGold> _poolSpawnGold;
        private FObjectPool<UIRewardEntryCustom> _poolCustom;
        private FObjectPool<UIRewardEntryList> _poolList;
        private FObjectPool<UIRewardEntryChest> _poolChest;

        private readonly Queue<IRewardEntry> _queueEntry = new();
        private IRewardEntry _currentEntry;
        private bool _anyEntryProcessed;

        private void Awake()
        {
            _poolFloating = new FObjectPool<UIRewardEntryFloating>(entryFloatingPref, transform);
            _poolSpawn = new FObjectPool<UIRewardEntrySpawn>(entrySpawnPref, transform);
            _poolSpawnGold = new FObjectPool<UIRewardEntrySpawnGold>(entrySpawnGoldPref, transform);
            _poolCustom = new FObjectPool<UIRewardEntryCustom>(entryCustomPref, transform);
            _poolList = new FObjectPool<UIRewardEntryList>(entryListPref, transform);
            _poolChest = new FObjectPool<UIRewardEntryChest>(entryChestPref, transform);
        }

        private void OnEnable()
        {
            GameEvent<((string, int)[], Vector3)>.Register(EVENT_ENTRY_FLOATING, DoEntryFloating, this);
            GameEvent<((string, int)[], Vector3, float)>.Register(EVENT_ENTRY_FLOATING, DoEntryFloating, this);
            GameEvent<((string, int)[], Vector3, string)>.Register(EVENT_ENTRY_FLOATING, DoEntryFloating, this);
            GameEvent<((string, int)[], Vector3, float, string)>.Register(EVENT_ENTRY_FLOATING, DoEntryFloating, this);

            GameEvent<(string, int)[]>.Register(GameKeys.REWARD_ENTRY_SPAWN, DoEntrySpawn, this);
            GameEvent<((string, int)[], string)>.Register(GameKeys.REWARD_ENTRY_SPAWN, DoEntrySpawn, this);
            GameEvent<(int, Vector3?)>.Register(GameKeys.REWARD_ENTRY_SPAWN_GOLD, DoEntrySpawnGold, this);
            GameEvent<(int, Vector3?, string)>.Register(GameKeys.REWARD_ENTRY_SPAWN_GOLD, DoEntrySpawnGold, this);

            GameEvent<Transform>.Register(EVENT_ENTRY_CUSTOM, DoEntryCustom, this);

            GameEvent<((string, int)[] rewards, string title, string description)>.Register(EVENT_ENTRY_LIST,
                DoEntryList, this);
            GameEvent<((string, int)[] rewards, string title, string description, Transform chest)>.Register(
                EVENT_ENTRY_CHEST, DoEntryChest, this);
            GameEvent<Action>.Register(EVENT_CALLBACK_CLOSE, SetCallbackClose, this);
        }

        private void OnDisable()
        {
            GameEvent<((string, int)[], Vector3)>.Unregister(EVENT_ENTRY_FLOATING, DoEntryFloating, this);
            GameEvent<((string, int)[], Vector3, float)>.Unregister(EVENT_ENTRY_FLOATING, DoEntryFloating, this);
            GameEvent<((string, int)[], Vector3, string)>.Unregister(EVENT_ENTRY_FLOATING, DoEntryFloating, this);
            GameEvent<((string, int)[], Vector3, float, string)>.Unregister(EVENT_ENTRY_FLOATING, DoEntryFloating, this);

            GameEvent<(string, int)[]>.Unregister(GameKeys.REWARD_ENTRY_SPAWN, DoEntrySpawn, this);
            GameEvent<((string, int)[], string)>.Unregister(GameKeys.REWARD_ENTRY_SPAWN, DoEntrySpawn, this);
            GameEvent<(int, Vector3?)>.Unregister(GameKeys.REWARD_ENTRY_SPAWN_GOLD, DoEntrySpawnGold, this);
            GameEvent<(int, Vector3?, string)>.Unregister(GameKeys.REWARD_ENTRY_SPAWN_GOLD, DoEntrySpawnGold, this);

            GameEvent<Transform>.Unregister(EVENT_ENTRY_CUSTOM, DoEntryCustom, this);

            GameEvent<((string, int)[] rewards, string title, string description)>.Unregister(EVENT_ENTRY_LIST,
                DoEntryList, this);
            GameEvent<((string, int)[] rewards, string title, string description, Transform chest)>.Unregister(
                EVENT_ENTRY_CHEST, DoEntryChest, this);
            GameEvent<Action>.Unregister(EVENT_CALLBACK_CLOSE, SetCallbackClose, this);
        }

        private void DoEntryChest(((string, int)[] rewards, string title, string description, Transform chest) obj)
        {
            var entry = _poolChest.Get();
            entry.Data = new RewardChestData
            { rewards = obj.rewards, title = obj.title, description = obj.description, chest = obj.chest };
            entry.transform.SetParent(transform);
            entry.transform.localPosition = Vector3.zero;
            entry.Init();
            entry.gameObject.SetActive(false);

            entry.OnDispose = () =>
            {
                if (ReferenceEquals(_currentEntry, entry)) _currentEntry = null;
                _poolChest.Release(entry);
            };
            entry.OnNext = OpenReward;
            _queueEntry.Enqueue(entry);
            OpenReward();
        }

        private void DoEntryList(((string id, int value)[] rewards, string title, string description) obj)
        {
            var entry = _poolList.Get();
            entry.Data = new RewardListData { rewards = obj.rewards, title = obj.title, description = obj.description };
            entry.transform.SetParent(transform);
            entry.transform.localPosition = Vector3.zero;
            entry.Init();
            entry.gameObject.SetActive(false);
            entry.OnDispose = () =>
            {
                if (ReferenceEquals(_currentEntry, entry)) _currentEntry = null;
                _poolList.Release(entry);
            };
            entry.OnNext = OpenReward;
            _queueEntry.Enqueue(entry);
            if (_currentEntry == null)
                OpenReward();
        }

        private void DoEntryCustom(Transform target)
        {
            var entry = _poolCustom.Get();
            entry.Data = new RewardCustomData { obj = target };
            entry.transform.SetParent(transform);
            entry.transform.localPosition = Vector3.zero;
            entry.Init();
            entry.gameObject.SetActive(false);
            entry.OnDispose = () =>
            {
                if (ReferenceEquals(_currentEntry, entry)) _currentEntry = null;
                _poolCustom.Release(entry);
            };
            entry.OnNext = OpenReward;
            _queueEntry.Enqueue(entry);
            OpenReward();
        }

        private void DoEntryFloating(((string, int)[] rewards, Vector3 position, float scaleItem, string where) data) =>
            SpawnFloating(new RewardFloatingData
            { rewards = data.rewards, position = data.position, scaleItem = data.scaleItem, where = data.where });

        private void DoEntryFloating(((string, int)[] rewards, Vector3 position, string where) data) =>
            SpawnFloating(new RewardFloatingData
            { rewards = data.rewards, position = data.position, where = data.where });

        private void DoEntryFloating(((string, int)[] rewards, Vector3 position, float scaleItem) data) =>
            DoEntryFloating((data.rewards, data.position, data.scaleItem, CurrentWhere()));

        private void DoEntryFloating(((string, int)[] rewards, Vector3 position) data) =>
            DoEntryFloating((data.rewards, data.position, CurrentWhere()));

        private void SpawnFloating(RewardFloatingData data)
        {
            var entry = _poolFloating.Get();
            entry.Data = data;
            entry.Init();
            entry.gameObject.SetActive(false);
            entry.OnDispose = () =>
            {
                if (ReferenceEquals(_currentEntry, entry)) _currentEntry = null;
                // Tra ve flow root: pool khong reset parent, de nguyen thi parent bi destroy keo chet ca instance trong pool
                entry.transform.SetParent(transform);
                _poolFloating.Release(entry);
            };
            entry.OnNext = OpenReward;
            _queueEntry.Enqueue(entry);
            OpenReward();
        }

        private void DoEntrySpawnGold((int amount, Vector3? position, string where) data)
        {
            RewardSpawnSingleData goldData = new()
            {
                amount = data.amount,
                position = data.position,
                where = data.where
            };

            if (goldData.amount > 0)
            {
                var entryGold = _poolSpawnGold.Get();
                entryGold.Data = goldData;
                entryGold.transform.SetParent(transform);
                entryGold.transform.localPosition = Vector3.zero;
                entryGold.Init();
                entryGold.gameObject.SetActive(false);
                entryGold.OnDispose = () =>
                {
                    if (ReferenceEquals(_currentEntry, entryGold)) _currentEntry = null;
                    // Tra ve flow root: pool khong reset parent, de nguyen thi parent bi destroy keo chet ca instance trong pool
                    entryGold.transform.SetParent(transform);
                    _poolSpawnGold.Release(entryGold);
                };
                entryGold.OnNext = OpenReward;
                _queueEntry.Enqueue(entryGold);
            }

            if (_currentEntry == null)
                OpenReward();
        }

        private void DoEntrySpawnGold((int amount, Vector3? position) data)
        {
            DoEntrySpawnGold((data.amount, data.position, CurrentWhere()));
        }

        private void DoEntrySpawn((string, int)[] reward)
        {
            DoEntrySpawn((reward, CurrentWhere()));
        }

        private void DoEntrySpawn(((string name, int amount)[] rewards, string where) data)
        {
            RewardSpawnSingleData goldData = new();
            RewardSpawnData otherData = new() { rewards = Array.Empty<(string, int)>() };

            foreach (var item in data.rewards)
            {
                if (item.amount == 0) continue;

                if (item.name == "gold")
                {
                    goldData.amount = item.amount;
                    goldData.where = data.where;
                    goldData.position = null;
                }
                else
                {
                    otherData.rewards = otherData.rewards.Append((item.name, item.amount)).ToArray();
                    otherData.where = data.where;
                }
            }

            if (goldData.amount > 0)
            {
                var entryGold = _poolSpawnGold.Get();
                entryGold.Data = goldData;
                entryGold.transform.SetParent(transform);
                entryGold.transform.localPosition = Vector3.zero;
                entryGold.Init();
                entryGold.gameObject.SetActive(false);
                entryGold.OnDispose = () =>
                {
                    if (ReferenceEquals(_currentEntry, entryGold)) _currentEntry = null;
                    // Tra ve flow root: pool khong reset parent, de nguyen thi parent bi destroy keo chet ca instance trong pool
                    entryGold.transform.SetParent(transform);
                    _poolSpawnGold.Release(entryGold);
                };
                entryGold.OnNext = OpenReward;
                _queueEntry.Enqueue(entryGold);
            }

            if (otherData.rewards.Length > 0)
            {
                var entry = _poolSpawn.Get();
                entry.Data = otherData;
                entry.transform.SetParent(transform);
                entry.transform.localPosition = Vector3.zero;
                entry.Init();
                entry.gameObject.SetActive(false);
                entry.OnDispose = () =>
                {
                    if (ReferenceEquals(_currentEntry, entry)) _currentEntry = null;
                    // Tra ve flow root: pool khong reset parent, de nguyen thi parent bi destroy keo chet ca instance trong pool
                    entry.transform.SetParent(transform);
                    _poolSpawn.Release(entry);
                };
                entry.OnNext = OpenReward;
                _queueEntry.Enqueue(entry);
            }

            if (_currentEntry == null)
                OpenReward();
        }

        private static string CurrentWhere() => GameRequest<string>.Request("current_tab_navigator");

        private void OpenReward()
        {
            if (_queueEntry.Count == 0)
            {
                if (_anyEntryProcessed)
                {
                    _anyEntryProcessed = false;
                    GameEvent.Emit(EVENT_FLOW_COMPLETE);
                }
                return;
            }

            _anyEntryProcessed = true;
            var entry = _queueEntry.Dequeue();
            entry.GameObject.SetActive(true);
            entry.Open();
            _currentEntry = entry;
        }

        private void SetCallbackClose(Action callback)
        {
            if (_currentEntry is UIRewardEntryList listEntry)
            {
                listEntry.SetCallbackClose(callback);
                return;
            }

            foreach (var entry in _queueEntry)
            {
                if (entry is UIRewardEntryList queuedListEntry)
                {
                    queuedListEntry.SetCallbackClose(callback);
                    return;
                }
            }
        }
    }
}