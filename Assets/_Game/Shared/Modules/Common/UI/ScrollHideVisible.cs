using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.Common
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollHideVisible : MonoBehaviour
    {
        [SerializeField] private float extraPadding = 32f;
        [SerializeField] private bool refreshOnEnable = true;

        private ScrollRect _scrollRect;
        private RectTransform _content;
        private RectTransform _viewport;
        private readonly List<ItemState> _items = new List<ItemState>();
        private readonly Vector3[] _itemCorners = new Vector3[4];
        private int _cachedChildCount = -1;

        private sealed class ItemState
        {
            public RectTransform rectTransform;
            public bool isHiddenByScroller;
            public bool isIgnoreHide;
        }
        
        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _viewport = _scrollRect.viewport;
            _content = _scrollRect.content;
        }

        private void OnEnable()
        {
            Subscribe();

            if (!refreshOnEnable)
                return;

            RefreshItems();
            RefreshVisibility();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetAllVisible();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled)
                return;

            RefreshVisibility();
        }

        private void LateUpdate()
        {
            if (!isActiveAndEnabled || _content == null)
                return;

            if (_cachedChildCount == _content.childCount)
                return;

            RefreshItems();
            RefreshVisibility();
        }

        public void RefreshItems()
        {
            SetAllVisible();
            _items.Clear();

            if (_content == null)
            {
                _cachedChildCount = -1;
                return;
            }

            for (int i = 0; i < _content.childCount; i++)
            {
                if (_content.GetChild(i) is not RectTransform child)
                    continue;

                _items.Add(new ItemState
                {
                    rectTransform = child,
                    isHiddenByScroller = false,
                    isIgnoreHide = child.GetComponent<IIgnoreHide>() != null,
                });
            }

            _cachedChildCount = _content.childCount;
        }

        public void RefreshVisibility()
        {
            if (_viewport == null || _content == null)
                return;

            if (_cachedChildCount != _content.childCount || _items.Count == 0)
                RefreshItems();

            Rect viewportRect = _viewport.rect;
            viewportRect.xMin -= extraPadding;
            viewportRect.xMax += extraPadding;
            viewportRect.yMin -= extraPadding;
            viewportRect.yMax += extraPadding;

            for (int i = 0; i < _items.Count; i++)
            {
                ItemState item = _items[i];
                if (item.rectTransform == null)
                    continue;

                if (!item.rectTransform.gameObject.activeSelf && !item.isHiddenByScroller)
                    continue;

                bool visible = IsVisibleInViewport(item.rectTransform, viewportRect);
                SetItemHidden(item, !visible);
            }
        }
        
        private void Subscribe()
        {
            if (_scrollRect == null)
                return;

            _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        private void Unsubscribe()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            RefreshVisibility();
        }

        private bool IsVisibleInViewport(RectTransform item, Rect viewportRect)
        {
            item.GetWorldCorners(_itemCorners);

            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                Vector3 localCorner = _viewport.InverseTransformPoint(_itemCorners[i]);
                minX = Mathf.Min(minX, localCorner.x);
                minY = Mathf.Min(minY, localCorner.y);
                maxX = Mathf.Max(maxX, localCorner.x);
                maxY = Mathf.Max(maxY, localCorner.y);
            }

            return maxX >= viewportRect.xMin &&
                   minX <= viewportRect.xMax &&
                   maxY >= viewportRect.yMin &&
                   minY <= viewportRect.yMax;
        }

        private static void SetItemHidden(ItemState item, bool hidden)
        {
            if (!item.rectTransform || item.isIgnoreHide)
                return;

            if (hidden)
            {
                if (item.isHiddenByScroller || !item.rectTransform.gameObject.activeSelf)
                    return;

                item.rectTransform.gameObject.SetActive(false);
                item.isHiddenByScroller = true;
                return;
            }

            if (!item.isHiddenByScroller)
                return;

            item.rectTransform.gameObject.SetActive(true);
            item.isHiddenByScroller = false;
        }

        private void SetAllVisible()
        {
            for (int i = 0; i < _items.Count; i++)
                SetItemHidden(_items[i], false);
        }
    }
    
    public interface IIgnoreHide {}
}
