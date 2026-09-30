/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-06
 */

using Cysharp.Threading.Tasks;
using DG.Tweening;
using Falcon.Helpers.EventBus;
using Falcon.Shared.Common;
using Falcon.Shared.PoolManager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Falcon.Shared.RewardFlow
{
    public class UIRewardEntrySpawn : RewardEntry<RewardSpawnData>
    {
        public UIRewardItem itemPrf;
        public RectTransform viewport;

        private FObjectPool<UIRewardItem> _poolItem;
        private FConfigRuntimeSO _config;
        private StaggeredDiamondLayout _layout;
        private readonly Dictionary<string, UIResource> _resources = new();

        private void Awake()
        {
            _config = Resources.Load<FConfigRuntimeSO>("ConfigRuntimeEntrySpawn");
            _poolItem = new FObjectPool<UIRewardItem>(itemPrf, viewport);
            _layout = viewport.GetComponent<StaggeredDiamondLayout>();
        }

        public override void Init()
        {
            base.Init();
            var parent = GameRequest<Transform>.Request($"falcon.reward.entry_parent_{Data.where}");
            if (parent)
            {
                transform.SetParent(parent);
            }

            _resources.Clear();

            foreach (var dataReward in Data.rewards)
            {
                var resource = UIResourceManager.GetResource(dataReward.name, Data.where);
                if (resource)
                {
                    _resources[dataReward.name] = resource;
                }
            }
        }

        public override async void Open()
        {
            var delay = _config.GetFloat("delay", 0.1f);
            var durationBetween = _config.GetFloat("durationBetween", 0.1f);
            var durationTweenScale = _config.GetFloat("durationTweenScale", 0.3f);
            var durationMove = _config.GetFloat("durationMove", 0.6f);
            var valueScale = _config.GetFloat("valueScale", 1.3f);
            var valueScaleWhileMove = _config.GetFloat("valueScaleWhileMove", 1f);
            var delayAfterScale = _config.GetFloat("delayAfterScale", 0.2f);
            var spacing = _config.GetFloat("spacing", 150);

            var tcs = this.GetCancellationTokenOnDestroy();
            var rewards = Data.rewards;

            _layout.enabled = true;
            _layout.horizontalSpacing = spacing;
            _layout.verticalSpacing = spacing;

            for (var i = 0; i < rewards.Length; i++)
            {
                var item = _poolItem.Get();
                item.SetItemData(rewards[i].name, rewards[i].amount);
#if UNITY_EDITOR
                item.name = $"{itemPrf.name}_{i}";
#endif
                item.transform.SetAsLastSibling();
                item.transform.localScale = Vector3.zero;
            }

            await UniTask.WaitForSeconds(delay, cancellationToken: tcs);

            _layout.enabled = false;

            for (var i = 0; i < rewards.Length; i++)
            {
                var nameReward = rewards[i].name;
                var amount = rewards[i].amount;
                var item = _poolItem.SpawnedObjects.ElementAt(i);
                var seq = DOTween.Sequence()
                    .SetLink(gameObject) // đổi scene: item chết theo entry, DOTween tự kill thay vì tween lên transform đã destroy
                    .Append(item.transform.DOScale(Vector3.one * valueScale, durationTweenScale).From(Vector3.zero).SetEase(Ease.OutBack))
                    .AppendInterval(delayAfterScale);

                if (_resources.TryGetValue(nameReward, out var resource))
                {
                    var localPos = transform.InverseTransformPoint(resource.icon.transform.position);
                    seq = seq.Append(item.transform.DOLocalMove(localPos, durationMove).SetEase(Ease.InBack))
                        .Join(item.transform.DOScale(Vector3.one * valueScaleWhileMove, durationMove).SetEase(Ease.OutQuad))
                        .AppendCallback(() =>
                        {
                            if (!this || !item) return;

                            // Counter ở scene khác, unload giữa chừng thì vẫn phải trả item về pool
                            if (resource && resource.icon)
                            {
                                resource.icon.transform.DOComplete();
                                resource.icon.transform.DOPunchScale(new Vector3(0.1f, -0.1f, 0), 0.5f, 1).SetTarget(resource.icon.transform).SetLink(resource.gameObject);
                            }
                            _poolItem.Release(item);
                            GameEvent.Emit($"falcon.ui.{nameReward}_update");
                            GameEvent<(string, int)>.Emit("falcon.ui.reward_count_up", (nameReward, amount));
                        });
                }
                else
                {
                    seq = seq.Append(item.transform.DOScale(Vector3.zero, durationTweenScale).SetEase(Ease.InBack))
                        .AppendCallback(() =>
                        {
                            if (!this || !item) return;

                            _poolItem.Release(item);
                            GameEvent.Emit($"falcon.ui.{nameReward}_update");
                            GameEvent<(string, int)>.Emit("falcon.ui.reward_count_up", (nameReward, amount));
                        });
                }
                await UniTask.WaitForSeconds(durationBetween, cancellationToken: tcs);
            }

            await UniTask.WaitUntil(IsAllItemReleased, cancellationToken: tcs);
            OnDispose?.Invoke();
            OnNext?.Invoke();
        }

        private bool IsAllItemReleased()
        {
            return _poolItem.CountActive == 0;
        }
    }

    public class RewardSpawnData : IRewardEntryData
    {
        public string where;
        public (string name, int amount)[] rewards;
    }
}