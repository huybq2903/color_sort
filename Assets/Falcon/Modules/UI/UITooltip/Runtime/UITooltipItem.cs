/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-15
 */

using System.Collections.Generic;
using DG.Tweening;
using Falcon.Helpers.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Falcon.Modules.UI.UITooltip.Runtime
{
    public class UITooltipItem : MonoBehaviour
    {
        [SerializeField] private Transform rewardPrefab, plusPrefab, grid;
        [SerializeField] private int maxItemsOneRow;
        [SerializeField] private SpriteAtlas atlas;
        [SerializeField] private TMP_Text txtContent;

        private List<Transform> _lsRewards, _lsPluses;
        private ObjectPool<Transform> _poolReward, _poolPlus;
        private GridLayoutGroup _gridLayoutGroup;
        private Image _imgBG;

        private void Awake()
        {
            _lsRewards = new List<Transform>();
            _lsPluses = new List<Transform>();
            _poolReward = new ObjectPool<Transform>(
                createFunc: () => Instantiate(rewardPrefab, grid),
                actionOnGet: trans => trans.gameObject.SetActive(true),
                actionOnRelease: trans => trans.gameObject.SetActive(false),
                actionOnDestroy: Destroy);
            _poolPlus = new ObjectPool<Transform>(
                createFunc: () => Instantiate(plusPrefab, grid),
                actionOnGet: trans => trans.gameObject.SetActive(true),
                actionOnRelease: trans => trans.gameObject.SetActive(false),
                actionOnDestroy: Destroy);

            _gridLayoutGroup = grid.GetComponent<GridLayoutGroup>();
            _imgBG = GetComponent<Image>();
        }

        private void Active((string, int)[] data)
        {
            // Đổi trigger thì Open() không đi qua Close(), tự trả pool ở đây kẻo item cũ dồn lại.
            DeActive();

            var rewardCount = data.Length;
            if (rewardCount <= 0)
            {
                // Không có quà thì đi tiếp nhánh text rỗng, kẻo bỏ qua scale và giữ nguyên hướng khung của lần trước.
                Active(string.Empty);
                return;
            }

            var itemsPerRow = CalculateItemsPerRow(rewardCount);
            var cols = Mathf.Max(1, 2 * itemsPerRow - 1);

            _gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayoutGroup.constraintCount = cols;

            var count = 0;
            foreach (var (nameIcon, amount) in data)
            {
                if (count % itemsPerRow > 0)
                {
                    var plus = _poolPlus.Get();
                    plus.SetAsLastSibling();
                    _lsPluses.Add(plus);
                }

                count++;
                var rewardItem = _poolReward.Get();
                var icon = rewardItem.GetComponentInChildren<Image>();
                var txtAmount = rewardItem.GetComponentInChildren<TMP_Text>();

                rewardItem.SetAsLastSibling();
                icon.sprite = atlas.GetSprite(nameIcon);
                txtAmount.text = GameRequest<(string, int), string>.Request("falcon.modules.format_quantity_reward", (nameIcon, amount));

                _lsRewards.Add(rewardItem);
            }

            grid.gameObject.SetActive(true);
            txtContent.SetText(string.Empty);
            transform.DOScale(1, 0.2f).From(0).SetEase(Ease.OutBack);
            Canvas.ForceUpdateCanvases();
            SetDirection();
        }

        private int CalculateItemsPerRow(int rewardCount)
        {
            var maxByConfig = maxItemsOneRow > 0 ? maxItemsOneRow : rewardCount;
            var maxCandidate = Mathf.Clamp(maxByConfig, 1, rewardCount);

            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (canvasRect == null || _gridLayoutGroup == null)
                return maxCandidate;

            var canvasRectArea = canvasRect.rect;
            var anchor = (Vector2)canvasRect.InverseTransformPoint(transform.position);

            var spaceLeft = Mathf.Max(0f, anchor.x - canvasRectArea.xMin);
            var spaceRight = Mathf.Max(0f, canvasRectArea.xMax - anchor.x);
            var spaceBottom = Mathf.Max(0f, anchor.y - canvasRectArea.yMin);
            var spaceTop = Mathf.Max(0f, canvasRectArea.yMax - anchor.y);

            var availableWidth = transform.localPosition.x > 0f ? spaceLeft : spaceRight;
            var availableHeight = transform.localPosition.y > 0f ? spaceBottom : spaceTop;
            availableWidth = Mathf.Max(0f, availableWidth);
            availableHeight = Mathf.Max(0f, availableHeight);

            var best = 1;
            for (var itemsPerRow = 1; itemsPerRow <= maxCandidate; itemsPerRow++)
            {
                if (FitsCanvas(rewardCount, itemsPerRow, availableWidth, availableHeight))
                    best = itemsPerRow;
            }

            return Mathf.Max(1, best);
        }

        private bool FitsCanvas(int rewardCount, int itemsPerRow, float availableWidth, float availableHeight)
        {
            var cols = Mathf.Max(1, 2 * itemsPerRow - 1);
            var rows = Mathf.CeilToInt((float)rewardCount / itemsPerRow);

            var padding = _gridLayoutGroup.padding;
            var cell = _gridLayoutGroup.cellSize;
            var spacing = _gridLayoutGroup.spacing;

            var requiredWidth = padding.left + padding.right + cols * cell.x + Mathf.Max(0, cols - 1) * spacing.x;
            var requiredHeight = padding.top + padding.bottom + rows * cell.y + Mathf.Max(0, rows - 1) * spacing.y;

            return requiredWidth <= availableWidth && requiredHeight <= availableHeight;
        }

        private void Active(string content)
        {
            DeActive();
            grid.gameObject.SetActive(false);
            txtContent.SetText(content);
            transform.DOScale(1, 0.2f).From(0).SetEase(Ease.OutBack);
            Canvas.ForceUpdateCanvases();
            SetDirection();
        }

        private void SetDirection()
        {
            var nameBgSprite = transform.localPosition switch
            {
                { x: > 0, y: <= 0 } => "BG_Tooltip_UpLeft",
                { x: > 0, y: > 0 } => "BG_Tooltip_DownLeft",
                { x: <= 0, y: > 0 } => "BG_Tooltip_DownRight",
                { x: <= 0, y: <= 0 } => "BG_Tooltip_UpRight",
                _ => ""
            };
            var sizeRect = _imgBG.rectTransform.sizeDelta;
            float deltaX;

            _imgBG.sprite = atlas.GetSprite(nameBgSprite);
            switch (transform.localPosition)
            {
                case { x: > 0, y: <= 0 }:
                    deltaX = _imgBG.sprite.rect.width - _imgBG.sprite.pivot.x;
                    _imgBG.rectTransform.pivot = new Vector2(1 - deltaX / sizeRect.x, 0);
                    break;
                case { x: > 0, y: > 0 }:
                    deltaX = _imgBG.sprite.rect.width - _imgBG.sprite.pivot.x;
                    _imgBG.rectTransform.pivot = new Vector2(1 - deltaX / sizeRect.x, 1);
                    break;
                case { x: <= 0, y: > 0 }:
                    deltaX = _imgBG.sprite.pivot.x;
                    _imgBG.rectTransform.pivot = new Vector2(deltaX / sizeRect.x, 1);
                    break;
                case { x: <= 0, y: <= 0 }:
                    deltaX = _imgBG.sprite.pivot.x;
                    _imgBG.rectTransform.pivot = new Vector2(deltaX / sizeRect.x, 0);
                    break;
                default:
                    _imgBG.rectTransform.pivot = new Vector2(0, 0);
                    break;
            }
        }

        private void DeActive()
        {
            foreach (var item in _lsRewards) _poolReward.Release(item);
            foreach (var item in _lsPluses) _poolPlus.Release(item);
            _lsRewards.Clear();
            _lsPluses.Clear();
        }
    }
}



