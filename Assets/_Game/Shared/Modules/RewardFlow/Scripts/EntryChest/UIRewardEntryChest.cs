/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-09
 */

using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Falcon.Helpers.EventBus;
using Falcon.Shared.Common;
using Falcon.Shared.PoolManager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.RewardFlow
{
    public class UIRewardEntryChest : RewardEntry<RewardChestData>
    {
        [SerializeField] private Transform gridTargets, startReward;
        
        public UIRewardItem item;
        public RectTransform slotReward;
        public RectTransform viewport;
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtDescription;
        public Button btnContinue;

        [Header("Tween")]
        public Image imgBackground;
        public RectTransform rectTMPAction;
        public RectTransform rectTMPTitle;

        [Header("Points")]
        public RectTransform targetChest;
        
        private FObjectPool<UIRewardItem> _poolReward;
        private FObjectPool<RectTransform> _poolSlotReward;
        private FConfigRuntimeSO _config;
        private Transform _oriParentChest;
        private Vector3 _oriPositionChest;
        private Transform _chest;
        
        private void Awake()
        {
            _config = Resources.Load<FConfigRuntimeSO>("ConfigRuntimeEntryChest");
            _poolReward = new FObjectPool<UIRewardItem>(item, viewport);
            _poolSlotReward = new FObjectPool<RectTransform>(slotReward, gridTargets);
            btnContinue.onClick.RemoveAllListeners();
            btnContinue.onClick.AddListener(OnClose);
        }
        
        public override void Open()
        {
            txtTitle.SetText(Data.title);
            txtDescription.SetText(Data.description);
            PreloadRewardAndPosition();
            btnContinue.gameObject.SetActive(false);
            rectTMPAction.transform.localScale = Vector3.zero;
            StartCoroutine(IESequence());
        }

        private IEnumerator IESequence()
        {
            yield return IEChestAppear();
            yield return IEChestJump(_chest.transform, targetChest.transform);
            yield return IEChestOpen();
        }
        
        private IEnumerator IEChestAppear()
        {
            var bgDurationFade = _config.GetFloat("bgDurationFade", 0.25f);
            var titleDurationScale = _config.GetFloat("titleDurationScale", 0.25f);
            var chestDurationScale = _config.GetFloat("chestDurationScale", 0.5f);
            
            // SetLink: đổi scene giữa chừng thì entry chết, DOTween tự kill thay vì tween lên object đã destroy
            imgBackground.DOFade(1, bgDurationFade).SetEase(Ease.InOutSine).From(0).SetLink(gameObject);
            rectTMPTitle.transform.DOScale(1, titleDurationScale).SetEase(Ease.OutBack).From(0).SetLink(gameObject);

            _oriParentChest = Data.chest.parent;
            _oriPositionChest = Data.chest.localPosition;
            _chest = Data.chest;
            _chest.SetParent(transform);
            _chest.localScale = Vector3.one;
            _chest.SetSiblingIndex(transform.childCount - 3);
            _chest.SendMessage("EntryStartAnimationIdle", SendMessageOptions.DontRequireReceiver);

            _chest.transform.DOScale(1f, chestDurationScale).SetEase(Ease.InQuad).SetLink(gameObject);
            yield return new WaitForEndOfFrame();
        }

        private IEnumerator IEChestJump(Transform chest, Transform target)
        {       
            var chestDelayJump = _config.GetFloat("chestDelayJump", 0.233f);
            var chestValueScale = _config.GetFloat("chestValueScale", 1.5f);
            var chestDurationScale = _config.GetFloat("chestDurationScale", 0.5f);
            var chestDurationJump = _config.GetFloat("chestDurationJump", 0.5f);
            var chestHeightJump = _config.GetFloat("chestHeightJump", 3f);
            var chestDelayTimeBound = _config.GetFloat("chestDelayTimeBound", 0.15f);
            var chestDurationBound = _config.GetFloat("chestDurationBound", 0.17f);
            var chestHighBound = _config.GetFloat("chestHighBound", 0.15f);
            
            _chest.SendMessage("EntryStartAnimationJump", SendMessageOptions.DontRequireReceiver);
            yield return new WaitForSeconds(chestDelayJump);
            _chest.transform.DOScale(chestValueScale, chestDurationScale).SetEase(Ease.InQuad).SetLink(gameObject);
            yield return JumpWorld(chest, target, chestHeightJump, chestDurationJump);
            var currentY = chest.position.y;
            yield return new WaitForSeconds(chestDelayTimeBound);
            yield return chest.DOMoveY(currentY + chestHighBound, chestDurationBound / 2).SetLink(gameObject).WaitForCompletion();
            yield return chest.DOMoveY(currentY, chestDurationBound / 2).SetLink(gameObject).WaitForCompletion();
        }

        private IEnumerator IEChestOpen()
        {
            _chest.SendMessage("EntryStartAnimationOpen", SendMessageOptions.DontRequireReceiver);
            
            var chestDelayOpen = _config.GetFloat("chestDelayOpen", 0.4f);
            var itemDurationScale = _config.GetFloat("itemDurationScale", 0.275f);
            var itemDurationMove = _config.GetFloat("itemDurationMove", 0.5f);
            var continueDurationScale = _config.GetFloat("continueDurationScale", 0.325f);
            var continueDelayScale = _config.GetFloat("continueDelayScale", 0.2f);
            var itemBetweenDelay = _config.GetFloat("itemBetweenDelay", 0.15f);
            
            yield return new WaitForSeconds(chestDelayOpen);

            var slots = new List<RectTransform>(_poolSlotReward.SpawnedObjects);
            var waitBetween = new WaitForSeconds(itemBetweenDelay);
            var index = 0;
            foreach (var clone in _poolReward.SpawnedObjects)
            {
                clone.transform.DOScale(1, itemDurationScale).SetEase(Ease.OutBack).SetLink(gameObject);
                clone.transform.DOMove(slots[index].transform.position, itemDurationMove)
                    .From(startReward.position)
                    .SetEase(Ease.OutBack)
                    .SetLink(gameObject);
                index++;
                yield return waitBetween;
            }
            
            btnContinue.gameObject.SetActive(true);
            rectTMPAction.transform.DOScale(1, continueDurationScale).SetEase(Ease.InOutSine).SetDelay(continueDelayScale).SetLink(gameObject);
        }

        private void PreloadRewardAndPosition()
        {
            _poolReward.Reset();
            _poolSlotReward.Reset();

            foreach (var dataRw in Data.rewards)
            {
                var itemRw = _poolReward.Get();
                itemRw.SetItemData(dataRw.id, dataRw.value);
                itemRw.transform.localScale = Vector3.zero;
                itemRw.transform.SetAsLastSibling();
                
                var slot = _poolSlotReward.Get();
                slot.transform.SetAsLastSibling();
            }
        }
        
        private void OnClose()
        {
            GameEvent.Emit("falcon.modules.ui.close_chest");
            StartCoroutine(IECloseUI());
            return;

            IEnumerator IECloseUI()
            {
                btnContinue.gameObject.SetActive(false);

                var bgDurationFade = _config.GetFloat("bgDurationFade", 0.25f);
                var titleDurationScale = _config.GetFloat("titleDurationScale", 0.25f);
                var chestDurationScale = _config.GetFloat("chestDurationScale", 0.5f);
                var continueDurationScale = _config.GetFloat("continueDurationScale", 0.325f);
                var itemDurationScale = _config.GetFloat("itemDurationScale", 0.275f);
                
                //Tween Out
                imgBackground.DOFade(0, bgDurationFade).SetEase(Ease.InOutSine).SetLink(gameObject);
                rectTMPTitle.transform.DOScale(0, titleDurationScale).SetEase(Ease.InBack).SetLink(gameObject);
                rectTMPAction.transform.DOScale(0, continueDurationScale).SetEase(Ease.InOutSine).SetLink(gameObject);

                foreach (var clone in _poolReward.SpawnedObjects) clone.transform.DOScale(0, itemDurationScale).SetEase(Ease.InBack).SetLink(gameObject);
                _chest.DOScale(0, chestDurationScale).SetEase(Ease.InQuad).SetLink(gameObject);

                yield return new WaitForSeconds(chestDurationScale);
                _chest.SetParent(_oriParentChest);
                _chest.localPosition = _oriPositionChest;
                _chest.SendMessage("EntryReturnChest", SendMessageOptions.DontRequireReceiver);
                GameEvent.Emit("falcon.modules.ui.after_close_chest");
                OnDispose?.Invoke();
                OnNext?.Invoke();
            }
        }
        
        protected virtual IEnumerator JumpWorld(Transform chest, Transform target, float jumpHeight, float duration)
        {
            Vector3 QuadBezier(Vector3 a, Vector3 b, Vector3 c, float t)
            {
                var ab = Vector3.Lerp(a, b, t);
                var bc = Vector3.Lerp(b, c, t);
                return Vector3.Lerp(ab, bc, t);
            }
            
            Vector3 start = chest.position;
            Vector3 end   = target.position;

            // Đỉnh nằm giữa theo XZ, và cao hơn điểm cao nhất + jumpHeight
            Vector3 mid = (start + end) * 0.5f;
            float apexY = Mathf.Max(start.y, end.y) + Mathf.Abs(jumpHeight);
            Vector3 control = new Vector3(mid.x, apexY, mid.z);

            // Animate t=0→1, cập nhật vị trí theo Bezier (world-space)
            yield return DOTween.To(() => 0f, t => {
                    chest.position = QuadBezier(start, control, end, t);
                }, 1f, duration)
                .SetEase(Ease.Linear) // giữ hình parabol chuẩn, không bị ease làm méo
                .SetLink(chest.gameObject)
                .WaitForCompletion();
        }
    }

    public class RewardChestData : IRewardEntryData
    {
        public (string id, int value)[] rewards;
        public string title;
        public string description;
        public Transform chest;
    }
}