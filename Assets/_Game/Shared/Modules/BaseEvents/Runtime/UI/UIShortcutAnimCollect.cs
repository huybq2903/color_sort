using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Falcon.Shared.Addressable;
using Falcon.Shared.Common;
using Falcon.Shared.PoolManager;
using Falcon.Shared.RewardFlow;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Falcon.Shared.BaseEvents
{
    public class UIShortcutAnimCollect : MonoBehaviour
    {
        [SerializeField] private UIRewardItem itemPrf;
        [SerializeField] private Sprite spriteCollect;

        private UIRewardItem _imageDouble;
        private UIRewardItem _textQuantity;
        private bool _isLeft;
        private FObjectPool<UIRewardItem> _poolItem;
        private FConfigRuntimeSO _config;
        private CancellationTokenSource _cts;
        private ParticleSystem _fxExplosionPrf;
        private FObjectPool<ParticleSystem> _poolFxExplosion;

        public void Setup(bool isLeft)
        {
            _isLeft = isLeft;
            _poolItem = new FObjectPool<UIRewardItem>(itemPrf, transform);
            _config = Resources.Load<FConfigRuntimeSO>("ConfigRuntimeShortcutAnimCollect");
            _fxExplosionPrf = AddressableExtensions.Load<GameObject>("FX_UI_TokenSparkleExplosion").GetComponent<ParticleSystem>();
            _poolFxExplosion = new FObjectPool<ParticleSystem>(_fxExplosionPrf, transform);
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            transform.DOKill();
            this.DOKill();
        }

        public async UniTask CoCollectAsync(int quantity, bool isDouble)
        {
            // Hủy task cũ nếu có và tạo mới
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var factor = _isLeft ? 1 : -1;

            // Tạo danh sách tasks để theo dõi tất cả spawn
            var tasks = new List<UniTask>(quantity);
            for (var i = 0; i < Mathf.Min(quantity, 5); i++)
            {
                tasks.Add(SpawnOneItemAndJumpAsync(new Vector3(180 * factor, 0, 0), i, token));
            }

            if (quantity > 1 || isDouble)
            {
                _textQuantity = _poolItem.Get();
                _textQuantity.transform.localScale = Vector3.one;
                _textQuantity.transform.localPosition = new Vector3(240 * factor, 0, 0);
                _textQuantity.icon.enabled = false;
                _textQuantity.txtValue.fontSize = 60;
                _textQuantity.txtValue.SetText($"+{quantity}");
                _textQuantity.transform.DOLocalMoveY(20, 0.5f).SetRelative();
                _textQuantity.GetComponent<CanvasGroup>().DoAlpha(1, 0.1f).From(0);
                await UniTask.WaitForSeconds(0.3f, cancellationToken: token);
                _textQuantity.GetComponent<CanvasGroup>().DoAlpha(0, 0.5f);
                await UniTask.WaitForSeconds(0.5f, cancellationToken: token);
                _poolItem.Release(_textQuantity);
            }
            await UniTask.WhenAll(tasks).AttachExternalCancellation(token);
        }

        private async UniTask SpawnOneItemAndJumpAsync(Vector3 originPos, int index, CancellationToken token)
        {
            var durationBetweenEachItem = _config.GetFloat("betweenEachItem", 0.1f);
            var itemRandomPos = _config.GetFloat("itemRandomPos", 10f);
            var durationFade = _config.GetFloat("durationFade", 0.1f);
            var valuePunchWhenAppear = _config.GetFloat("valuePunchWhenAppear", 0.15f);
            var durationPunchWhenAppear = _config.GetFloat("durationPunchWhenAppear", 0.2f);
            var delayBeforeJump = _config.GetFloat("delayBeforeJump", 0.1f);
            var durationBoundBeforeJump = _config.GetFloat("durationBoundBeforeJump", 0.4f);
            var durationJump = _config.GetFloat("durationJump", 0.4f);
            var powerJump = _config.GetFloat("powerJump", 140);
            var easeJump = _config.GetEase("easeJump", Ease.InCubic);

            await UniTask.WaitForSeconds(index * durationBetweenEachItem, cancellationToken: token);

            var imageCollect = _poolItem.Get();
            imageCollect.transform.localPosition = originPos + new Vector3(Random.Range(-itemRandomPos, itemRandomPos),
                                                       Random.Range(-itemRandomPos, itemRandomPos), 0);
            imageCollect.transform.localScale = Vector3.one;
            imageCollect.icon.sprite = spriteCollect;
            imageCollect.icon.enabled = true;
            imageCollect.txtValue.SetText(string.Empty);
            imageCollect.GetComponent<CanvasGroup>().DoAlpha(1, durationFade).From(0);
            await UniTask.WaitForSeconds(durationFade, cancellationToken: token);
            imageCollect.transform.DOPunchScale(Vector3.up * valuePunchWhenAppear + Vector3.left * valuePunchWhenAppear,
                durationPunchWhenAppear, 2);

            await UniTask.WaitForSeconds(durationPunchWhenAppear + delayBeforeJump, cancellationToken: token);

            imageCollect.transform.DOScale(new Vector3(1.2f, 0.8f), durationBoundBeforeJump);
            await UniTask.WaitForSeconds(durationBoundBeforeJump, cancellationToken: token);
            imageCollect.transform.DOScale(new Vector3(0.9f, 1.1f), 0.1f);
            imageCollect.transform.DOLocalJump(Vector3.zero, powerJump, 1, durationJump).SetEase(easeJump);
            await UniTask.WaitForSeconds(0.1f, cancellationToken: token);
            imageCollect.transform.DOScale(Vector3.one, 0.2f);
            await UniTask.WaitForSeconds(durationJump - 0.1f, cancellationToken: token);

            _poolItem.Release(imageCollect);

            this.DOComplete();
            var fxExplosion = _poolFxExplosion.Get();
            fxExplosion.transform.localPosition = Vector3.zero;
            fxExplosion.Play();
            fxExplosion.GetComponent<FParticleListener>().OnParticleStop = () => _poolFxExplosion.Release(fxExplosion);
            transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 4).SetTarget(this);
            await UniTask.WaitForSeconds(0.2f, cancellationToken: token);
        }
    }
}