/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-07
 */

using Cysharp.Threading.Tasks;
using DG.Tweening;
using Falcon.Helpers.EventBus;
using Falcon.Shared.Addressable;
using Falcon.Shared.Common;
using Falcon.Shared.PoolManager;
using TMPro;
using UnityEngine;

namespace Falcon.Shared.RewardFlow
{
    public class UIRewardEntrySpawnGold : RewardEntry<RewardSpawnSingleData>
    {
        public Transform itemPrf, viewPort;
        public TMP_Text textAmount;

        private FObjectPool<Transform> _poolItem;
        private FConfigRuntimeSO _config;
        private UIResource _resource;
        private CanvasGroup _canvasGroupText, _canvasGroupResource;
        private FObjectPool<ParticleSystem> _poolFxExplosion;
        private ParticleSystem _fxExplosionPrf;

        private void Awake()
        {
            _config = Resources.Load<FConfigRuntimeSO>("ConfigRuntimeEntrySpawnGold");
            _poolItem = new FObjectPool<Transform>(itemPrf, viewPort);
            _canvasGroupText = textAmount.GetComponent<CanvasGroup>();
            _fxExplosionPrf = AddressableExtensions.Load<GameObject>("FX_UI_Coin_Claim").GetComponent<ParticleSystem>();
            _poolFxExplosion = new FObjectPool<ParticleSystem>(_fxExplosionPrf, transform);
        }

        public override void Init()
        {
            base.Init();
            var parent = GameRequest<Transform>.Request($"falcon.reward.entry_parent_{Data.where}");
            if (parent)
            {
                transform.SetParent(parent);
            }

            _resource = UIResourceManager.GetResource("gold", Data.where);
            _canvasGroupResource = null;
            if (_resource)
            {
                _canvasGroupResource = _resource.GetComponent<CanvasGroup>();
            }

            _canvasGroupText.alpha = 0f;
        }

        /// <summary>Text +amount bay lên rồi mờ dần, chạy song song với đàn xu.</summary>
        private void FloatAmount()
        {
            if (!textAmount) return;

            var textDurationMoveY = _config.GetFloat("textDurationMoveY", 1.2f);
            var textValueMoveY = _config.GetFloat("textValueMoveY", 50f);
            var textStartMoveY = _config.GetFloat("textStartMoveY", -70f);
            var textDurationFade = _config.GetFloat("textDurationFade", 0.3f);

            textAmount.SetText($"+{Data.amount}");
            if (Data.position.HasValue)
                textAmount.transform.position = Data.position.Value;
            else
                textAmount.transform.localPosition = Vector3.zero;
            textAmount.transform
                .DOLocalMoveY(textAmount.transform.localPosition.y + textValueMoveY, textDurationMoveY)
                .From(textAmount.transform.localPosition.y + textStartMoveY).SetEase(Ease.Linear)
                .SetLink(gameObject);

            DOTween.Sequence()
                .Append(_canvasGroupText.DOFade(1, textDurationFade))
                .AppendInterval(textDurationMoveY - textDurationFade * 2)
                .Append(_canvasGroupText.DOFade(0, textDurationFade))
                .SetLink(gameObject);
        }

        public override async void Open()
        {
            if (!_resource)
            {
                // Scene khong co counter gold: chay het text +amount roi moi dong, return tran thi entry ket va chan queue
                FloatAmount();
                var textDuration = _config.GetFloat("textDurationMoveY", 1.2f);
                var cancelled = await UniTask
                    .WaitForSeconds(textDuration, cancellationToken: this.GetCancellationTokenOnDestroy())
                    .SuppressCancellationThrow();
                if (cancelled) return;

                OnNext?.Invoke();
                OnDispose?.Invoke();
                return;
            }

            var maxAmount = _config.GetInt("maxAmount", 7);
            var delayStart = _config.GetFloat("delayStart", 0.15f);
            var delayBetween = _config.GetFloat("delayBetween", 0.11f);
            var durationAppearOne = _config.GetFloat("durationAppearOne", 0.3f);
            var delayToMoveOne = _config.GetFloat("delayToMoveOne", 0f);
            var durationToMoveOne = _config.GetFloat("durationToMoveOne", 0.8f);
            var rangeRandomX = _config.GetFloat("rangeRandomX", 50f);
            var rangeRandomY = _config.GetFloat("rangeRandomY", 10f);
            var curveMoveY = _config.GetCurve("curveMoveY");
            var curveMoveX = _config.GetCurve("curveMoveX");

            var tcs = this.GetCancellationTokenOnDestroy();
            var amount = Mathf.Min(Data.amount, maxAmount);

            if (_canvasGroupResource)
            {
                _canvasGroupResource.DOFade(1, 0.2f).From(0f).SetLink(_canvasGroupResource.gameObject);
            }

            FloatAmount();

            await UniTask.WaitForSeconds(delayStart, cancellationToken: tcs);

            // Counter gold nằm ở scene khác, có thể unload trong lúc chờ
            if (!_resource)
            {
                OnNext?.Invoke();
                OnDispose?.Invoke();
                return;
            }

            var isSetTextResource = false;
            var localPos = transform.InverseTransformPoint(_resource.icon.transform.position);

            for (var i = 0; i < amount; i++)
            {
                var item = _poolItem.Get();
                item.SetAsFirstSibling();
                item.localScale = Vector3.zero;
                if (Data.position.HasValue)
                    item.position = Data.position.Value;
                else
                    item.localPosition = Vector3.zero;
                item.localPosition += new Vector3(Random.Range(-rangeRandomX, rangeRandomX), Random.Range(-rangeRandomY, rangeRandomY), 0);

                DOTween.Sequence()
                    .Append(item.DOScale(1, durationAppearOne))
                    .AppendInterval(delayToMoveOne)
                    .Append(item.DOLocalMoveX(localPos.x, durationToMoveOne).SetEase(curveMoveX))
                    .Join(item.DOLocalMoveY(localPos.y, durationToMoveOne).SetEase(curveMoveY))
                    .Join(item.DOScale(Vector3.one, durationToMoveOne).SetEase(Ease.OutQuad))
                    .AppendCallback(() =>
                    {
                        if (!isSetTextResource)
                        {
                            isSetTextResource = true;
                            GameEvent.Emit(GameKeys.GOLD_UPDATE);
                        }

                        GameEvent<string>.Emit(GameKeys.PLAY_SFX, "ClaimGold");
                        _resource.DOComplete();
                        _resource.icon.transform.DOPunchScale(Vector3.one * 0.2f, 0.1f).SetEase(Ease.OutBack)
                            .SetTarget(_resource).SetLink(_resource.gameObject);
                        _poolItem.Release(item);


                        var fxExplosion = _poolFxExplosion.Get();
                        fxExplosion.transform.position = _resource.icon.transform.position;
                        fxExplosion.Play();
                        fxExplosion.GetComponent<FParticleListener>().OnParticleStop = () => _poolFxExplosion.Release(fxExplosion);
                    })
                    .SetLink(gameObject);

                await UniTask.WaitForSeconds(delayBetween, cancellationToken: tcs);
            }
            await UniTask.WaitUntil(IsAllItemReleased, cancellationToken: tcs);
            OnNext?.Invoke();
            await UniTask.WaitUntil(IsAllFxReleased, cancellationToken: tcs);
            OnDispose?.Invoke();
        }

        private bool IsAllItemReleased() => _poolItem.CountActive == 0;
        private bool IsAllFxReleased() => _poolFxExplosion.CountActive == 0;
    }

    public class RewardSpawnSingleData : IRewardEntryData
    {
        public string where;
        public int amount;
        public Vector3? position;
    }
}