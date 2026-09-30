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
using UnityEngine;

namespace Falcon.Shared.RewardFlow
{
    public class UIRewardEntryFloating : RewardEntry<RewardFloatingData>
    {
        public UIRewardItem itemPrf;
        public RectTransform viewport;

        private FObjectPool<UIRewardItem> _poolItem;
        private FConfigRuntimeSO _config;

        private void Awake()
        {
            _config = Resources.Load<FConfigRuntimeSO>("ConfigRuntimeEntryFloating");
            _poolItem = new FObjectPool<UIRewardItem>(itemPrf, viewport);
        }

        public override void Init()
        {
            var parent = GameRequest<Transform>.Request($"falcon.reward.entry_parent_{Data.where}");
            if (parent) transform.SetParent(parent);
        }

        public override async void Open()
        {
            var delay = _config.GetFloat("delay", 0.1f);
            var durationBetween = _config.GetFloat("durationBetween", 0.1f); // key chưa có trong SO
            var durationTweenScale = _config.GetFloat("durationTweenScale", 0.3f);
            var durationTweenMoveY = _config.GetFloat("durationTweenMoveY", 0.5f);
            var durationTweenFade = _config.GetFloat("durationTweenFade", 0.25f);
            var delayTweenMoveY = _config.GetFloat("delayTweenMoveY", 0.5f);
            var valueRelativeMoveY = _config.GetFloat("valueRelativeMoveY", 1f);
            var tcs = this.GetCancellationTokenOnDestroy();
            await UniTask.WaitForSeconds(delay, cancellationToken: tcs);
            var positions = CalculateRewardPositions(Data.position, Data.rewards.Length, Data.scaleItem);
            var rewards = Data.rewards;
            for (var i = 0; i < rewards.Length; i++)
            {
                var nameReward = rewards[i].name;
                var item = _poolItem.Get();
                GameEvent.Emit($"falcon.ui.{nameReward}_update");
                item.SetItemData(nameReward, rewards[i].amount);
                item.transform.position = positions[i];
                // SetLink: đổi scene giữa chừng thì item chết theo entry, DOTween tự kill thay vì tween lên transform đã destroy
                item.transform.DOScale(Data.scaleItem, durationTweenScale).From(0).SetEase(Ease.OutBack)
                    .SetLink(gameObject);
                item.transform.DOMoveY(valueRelativeMoveY, durationTweenMoveY)
                    .SetDelay(delayTweenMoveY)
                    .SetRelative(true)
                    .SetEase(Ease.InOutSine)
                    .OnComplete(() =>
                    {
                        _poolItem.Release(item);
                    })
                    .SetLink(gameObject);
                item.canvasGroup.DOFade(0, durationTweenFade)
                    .From(1)
                    .SetDelay(delayTweenMoveY + durationTweenMoveY - durationTweenFade)
                    .SetLink(gameObject);
#if UNITY_EDITOR
                item.name = $"{itemPrf.name}_{i}";
#endif
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

        private static Vector3[] CalculateRewardPositions(Vector3 centerPosition, int count, float scaleItem = 0.8f)
        {
            var positions = new Vector3[count];

            if (count == 1)
            {
                // 1 reward: ở giữa, hơi cao lên một chút
                positions[0] = centerPosition + Vector3.up * 0.5f;
                return positions;
            }

            // Nhiều reward: dàn đều theo chiều ngang
            const float spacing = 0.6f; // Khoảng cách giữa các reward

            var totalWidth = (count - 1) * spacing * scaleItem / 0.8f;
            var startX = centerPosition.x - totalWidth / 2f;

            for (var i = 0; i < count; i++)
            {
                var x = startX + i * spacing * scaleItem / 0.8f;
                positions[i] = new Vector3(x, centerPosition.y + 0.5f, centerPosition.z);
            }

            return positions;
        }
    }

    public class RewardFloatingData : IRewardEntryData
    {
        public (string name, int amount)[] rewards;
        public Vector3 position;
        public float scaleItem = 0.8f;
        public string where;
    }
}