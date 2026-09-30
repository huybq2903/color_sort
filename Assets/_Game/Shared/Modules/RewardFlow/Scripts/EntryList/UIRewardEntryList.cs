/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-09
 */

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Falcon.Shared.Common;
using Falcon.Shared.PoolManager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.RewardFlow
{
    public class UIRewardEntryList : RewardEntry<RewardListData>
    {
        public UIRewardItem item;
        public RectTransform viewport;
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtDescription;
        public Button btnContinue;

        [Header("Tween")]
        public Image imgBackground;
        public RectTransform rectTMPAction;
        public RectTransform rectTMPTitle;
        public RectTransform rectTMPDescription;

        [Header("Points")]
        public List<RectTransform> points;

        private Action _callbackClose;

        public void SetCallbackClose(Action callback) => _callbackClose = callback;
        protected int _currentPage = 0;
        protected int _totalPages = 0;
        
        private FObjectPool<UIRewardItem> _poolItem;
        private FConfigRuntimeSO _config;
        
        private void Awake()
        {
            _poolItem = new FObjectPool<UIRewardItem>(item, viewport);
            _config = Resources.Load<FConfigRuntimeSO>("ConfigRuntimeEntryList");
        }

        public override void Init()
        {
            btnContinue.onClick.RemoveAllListeners();
            btnContinue.onClick.AddListener(ShowNextPage);
            _isRunTweenIn = false;
        }

        public override void Open()
        {
            txtTitle.SetText(Data.title);
            txtDescription.SetText(Data.description);

            //Show First Page
            _currentPage = 0;
            _totalPages = Mathf.CeilToInt((float)Data.rewards.Length / points.Count);
            StartCoroutine(IESpawnPage(_currentPage));
        }

        private bool _isRunTweenIn = false;
        
        private int SetPointsActiveRange(int needed)
        {
            int activeCount = Mathf.Clamp(needed, 0, points.Count);
            for (int i = 0; i < points.Count; i++)
                points[i].gameObject.SetActive(i < activeCount);
            return activeCount;
        }

        private IEnumerator IESpawnPage(int pageIndex)
        {
            var delayEachItem = _config.GetFloat("delayEachItem", 0.05f);
            var durationTweenFadeBackground = _config.GetFloat("durationTweenFadeBackground", 0.25f);
            var durationTweenScaleTitle = _config.GetFloat("durationTweenScaleTitle", 0.25f);
            var durationTweenScaleDescription = _config.GetFloat("durationTweenScaleDescription", 0.325f);
            var durationTweenScaleContinue = _config.GetFloat("durationTweenScaleContinue", 0.325f);
            var durationTweenScaleItem = _config.GetFloat("durationTweenScaleItem", 0.275f);
            var durationTweenFadeContinue = _config.GetFloat("durationTweenFadeContinue", 0.25f);
            
            btnContinue.gameObject.SetActive(false);
            _poolItem.Reset();

            var wait = new WaitForSeconds(delayEachItem);

            int itemsPerPage = points.Count;
            int start = pageIndex * itemsPerPage;
            int end = Mathf.Min(start + itemsPerPage, Data.rewards.Length);

            //Tween In
            if (!_isRunTweenIn)
            {
                // SetLink: đổi scene giữa chừng thì entry chết, DOTween tự kill thay vì tween lên object đã destroy
                imgBackground.DOFade(0.99f, durationTweenFadeBackground).SetEase(Ease.InOutSine).From(0).SetLink(gameObject);
                rectTMPTitle.transform.DOScale(1, durationTweenScaleTitle).SetEase(Ease.OutBack).From(0).SetLink(gameObject);
                rectTMPDescription.transform.DOScale(1, durationTweenScaleDescription).SetEase(Ease.OutBack).From(0).SetLink(gameObject);
                rectTMPAction.transform.DOScale(1, durationTweenScaleContinue).SetEase(Ease.InOutSine).From(0).SetDelay(durationTweenScaleDescription).SetLink(gameObject);

                //Cache
                _isRunTweenIn = true;
            }

            //State Points Active / Deactive
            int itemsThisPage = end - start;
            SetPointsActiveRange(itemsThisPage);
            yield return new WaitForEndOfFrame();

            for (int i = start; i < end; i++)
            {
                var cloneItem = _poolItem.Get();
                cloneItem.SetItemData(Data.rewards[i].id, (int)Data.rewards[i].value);
                cloneItem.gameObject.SetActive(true);
                cloneItem.name = $"{item.name}_{i}";
                cloneItem.transform.DOScale(1, durationTweenScaleItem).From(0).SetEase(Ease.OutBack).SetLink(gameObject);
                
                int pointIndex = (i - start) % points.Count;
                if (pointIndex < points.Count)
                {
                    cloneItem.transform.position = points[pointIndex].position;
                }

                yield return wait;
            }

            yield return new WaitForSeconds(durationTweenFadeContinue);
            btnContinue.gameObject.SetActive(true);
        }

        private void ShowNextPage()
        {
            var durationTweenFadeBackground = _config.GetFloat("durationTweenFadeBackground", 0.25f);
            var durationTweenScaleTitle = _config.GetFloat("durationTweenScaleTitle", 0.25f);
            var durationTweenScaleDescription = _config.GetFloat("durationTweenScaleDescription", 0.325f);
            var durationTweenScaleContinue = _config.GetFloat("durationTweenScaleContinue", 0.325f);
            var durationTweenScaleItem = _config.GetFloat("durationTweenScaleItem", 0.275f);
            var durationTweenFadeContinue = _config.GetFloat("durationTweenFadeContinue", 0.25f);
            
            if (_currentPage + 1 < _totalPages)
            {
                _currentPage++;
                StartCoroutine(IESpawnPage(_currentPage));
            }
            else
            {
                StartCoroutine(IECloseUI());
                IEnumerator IECloseUI()
                {
                    btnContinue.gameObject.SetActive(false);
                    
                    //Tween Out
                    imgBackground.DOFade(0, durationTweenFadeBackground).SetEase(Ease.InOutSine).SetLink(gameObject);
                    rectTMPTitle.transform.DOScale(0, durationTweenScaleTitle).SetEase(Ease.InBack).SetLink(gameObject);
                    rectTMPDescription.transform.DOScale(0, durationTweenScaleDescription).SetEase(Ease.InBack).SetLink(gameObject);
                    rectTMPAction.transform.DOScale(0, durationTweenScaleContinue).SetEase(Ease.InOutSine).SetLink(gameObject);

                    foreach (var clone in _poolItem.SpawnedObjects) clone.transform.DOScale(0, durationTweenScaleItem / 2).SetEase(Ease.InQuad).SetLink(gameObject);

                    yield return new WaitForSeconds(durationTweenFadeContinue);
                    OnDispose?.Invoke();
                    OnNext?.Invoke();
                    _callbackClose?.Invoke();
                    _callbackClose = null;
                }
            }
        }
    }

    public class RewardListData : IRewardEntryData
    {
        public (string id, int value)[] rewards;
        public string title;
        public string description;
    }
}