/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-01
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Falcon.Modules.Packs.PacksFailOffer.Runtime
{
    /// <summary>
    /// Class dùng cho các group pack có scroll kéo ngang
    /// Muốn dùng thì viết 1 class tổng bọc component chứa class này lại
    /// </summary>
    public class UIBannerGroupPack : MonoBehaviour
    {
        [SerializeField] private RectTransform itemWrapperPrefab;
        [SerializeField] private Toggle togglePrefab;
        [SerializeField] private Transform gridContent, gridToggle, pool;
        [SerializeField] private bool autoScroll;
        [SerializeField, ShowIf("autoScroll")] private float timeBetween;
        
        private ScrollRect _scrollRect;
        private ToggleGroup _toggleGroup;
        private IEnumerator _coAutoScroll;
        
        /// <summary>
        /// Chứa các item đang có trong group (bao gồm item đã ẩn), mỗi item gồm pack và toggle (dấu chấm bên dưới scroll)
        /// </summary>
        private readonly Dictionary<string, (RectTransform pack, Toggle toggle)> _dictItem = new();

        private void Awake() => Initialize();

        public void Initialize()
        {
            _scrollRect = GetComponentInChildren<ScrollRect>();
            _toggleGroup = GetComponentInChildren<ToggleGroup>();
        }

        /// <summary>
        /// Lấy ra item bao gồm pack và toggle
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public (RectTransform pack, Toggle toggle) GetItem(string key)
        {
            return _dictItem.GetValueOrDefault(key);
        }

        /// <summary>
        /// Thêm pack vào trong group pack, nếu đã có thì bật lên, chưa có thì khởi tạo
        /// </summary>
        /// <param name="key">key để phân biệt pack</param>
        /// <param name="asset">asset lấy từ addresable</param>
        public bool Add(string key, AssetReference asset)
        {
            if (_dictItem.TryGetValue(key, out var itemP))
            {
                itemP.pack.SetParent(gridContent);
                itemP.pack.gameObject.SetActive(true);
                itemP.toggle.transform.SetParent(gridToggle);
                itemP.toggle.gameObject.SetActive(true);
                itemP.toggle.group = _toggleGroup;
            }
            else
            {
                var itemWrapper = Instantiate(itemWrapperPrefab, pool);
                itemWrapper.sizeDelta = new Vector2(_scrollRect.viewport.rect.size.x, 0);

                var handleIns = asset.InstantiateAsync(itemWrapper);
                var result = handleIns.WaitForCompletion();
                if (!result) return false;
                var goPack = result.transform;
                goPack.SendMessage("Setup", key, SendMessageOptions.DontRequireReceiver);
                goPack.SetParent(itemWrapper);
                goPack.localPosition = Vector3.zero;
                itemWrapper.SetParent(gridContent);

                var toggle = Instantiate(togglePrefab, gridToggle);
                toggle.group = _toggleGroup;

                _dictItem[key] = (itemWrapper, toggle);
            }

            foreach (var item in _dictItem.Values)
            {
                item.toggle.onValueChanged.RemoveAllListeners();
            }

            return true;
        }

        public void Remove(string key)
        {
            if (!_dictItem.TryGetValue(key, out var item)) return;
            
            item.pack.SetParent(pool);
            item.pack.gameObject.SetActive(false);
            item.toggle.transform.SetParent(pool);
            item.toggle.gameObject.SetActive(false);
            item.toggle.group = null;
            Canvas.ForceUpdateCanvases();
            
            foreach (var it in _dictItem.Values)
            {
                it.toggle.onValueChanged.RemoveAllListeners();
            }
        }

        /// <summary>
        /// Update layout cho scroll, dùng khi Add hoặc Remove pack
        /// </summary>
        public void UpdateScroll()
        {
            if (gameObject.activeInHierarchy)
                _scrollRect.SendMessage("Setup");
            DoCheckChildCount();
        }

        private IEnumerator CoAutoScroll()
        {
            if (autoScroll && timeBetween > 0)
            {
                yield return new WaitForSeconds(timeBetween);
                if (gameObject.activeInHierarchy) _scrollRect.SendMessage("GoToNextPanel");
            }
        }

        private void DoCheckChildCount()
        {
            if (QuantityPackShowing <= 1)
            {
                gridToggle.gameObject.SetActive(false);
                _scrollRect.enabled = false;
            }
            else
            {
                gridToggle.gameObject.SetActive(true);
                _scrollRect.enabled = true;
            }

            RestartAutoScroll();
        }

        public void RestartAutoScroll()
        {
            if (_coAutoScroll != null) StopCoroutine(_coAutoScroll);
            _coAutoScroll = CoAutoScroll();
            if (gridContent.childCount > 1)
            {
                StartCoroutine(_coAutoScroll);
            }
        }

        public int QuantityPackShowing => gridContent.childCount;
    }
}