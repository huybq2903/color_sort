/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-02-06
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Shared.RewardFlow
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class StaggeredDiamondLayout : LayoutGroup
    {
        [Min(1)] public int smallRowCount = 1;
        [Min(1)] public int largeRowCount = 2;

        public float horizontalSpacing = 120f;
        public float verticalSpacing = 120f;

        public override void CalculateLayoutInputVertical() { }

        public override void SetLayoutHorizontal() => Arrange();
        public override void SetLayoutVertical() => Arrange();

        private void Arrange()
        {
            m_Tracker.Clear();

            int total = rectChildren.Count;
            if (total == 0) return;

            var rowCounts = BuildRowCounts(total);
            if (rowCounts.Count == 0) return;

            float totalHeight = (rowCounts.Count - 1) * verticalSpacing;
            float startY = totalHeight * 0.5f;

            int index = 0;
            for (int row = 0; row < rowCounts.Count; row++)
            {
                int count = rowCounts[row];
                float y = startY - row * verticalSpacing;
                float startX = -((count - 1) * horizontalSpacing) * 0.5f;

                for (int i = 0; i < count; i++)
                {
                    if (index >= total) return;

                    RectTransform child = rectChildren[index++];
                    m_Tracker.Add(this, child,
                        DrivenTransformProperties.AnchoredPosition |
                        DrivenTransformProperties.Anchors |
                        DrivenTransformProperties.Pivot);

                    child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
                    child.anchoredPosition = new Vector2(startX + i * horizontalSpacing, y);
                }
            }
        }

        private List<int> BuildRowCounts(int total)
        {
            int small = Mathf.Max(1, smallRowCount);
            int large = Mathf.Max(1, largeRowCount);
            if (small == large)
            {
                large = small + 1;
            }

            if (small > large)
            {
                (small, large) = (large, small);
            }

            var diamond = TryBuildDiamond(total);
            if (diamond != null) return diamond;

            bool preferStartLarge = total % 2 == 1;
            var rows = TryBuildWithStart(total, small, large, preferStartLarge);
            if (rows != null) return rows;

            rows = TryBuildWithStart(total, small, large, !preferStartLarge);
            return rows ?? new List<int> { total };
        }

        private List<int> TryBuildWithStart(int total, int small, int large, bool startLarge)
        {
            int start = startLarge ? large : small;
            int other = startLarge ? small : large;
            int maxRows = Mathf.CeilToInt((float)total / Mathf.Min(small, large));

            bool requireEndStart = startLarge && total % 2 == 1;
            var exact = TryBuildExact(total, start, other, maxRows, requireEndStart);
            if (exact != null) return exact;

            if (requireEndStart)
            {
                exact = TryBuildExact(total, start, other, maxRows, false);
                if (exact != null) return exact;
            }

            return TryBuildWithRemainder(total, start, other, maxRows);
        }

        private List<int> TryBuildExact(int total, int start, int other, int maxRows, bool requireEndStart)
        {
            for (int length = maxRows; length >= 1; length--)
            {
                if (requireEndStart && length % 2 == 0) continue;
                if (SumAlternating(length, start, other) != total) continue;
                return BuildSequence(length, start, other);
            }

            return null;
        }

        private List<int> TryBuildWithRemainder(int total, int start, int other, int maxRows)
        {
            for (int length = maxRows; length >= 0; length--)
            {
                int sum = SumAlternating(length, start, other);
                if (sum >= total) continue;

                int remainder = total - sum;
                if (length == 0)
                {
                    return new List<int> { remainder };
                }

                int last = (length % 2 == 1) ? start : other;
                int prev = length >= 2 ? ((length % 2 == 1) ? other : start) : int.MinValue;

                if (remainder != last)
                {
                    var rows = BuildSequence(length, start, other);
                    rows.Add(remainder);
                    return rows;
                }

                int merged = last + remainder;
                if (merged == prev) continue;

                var mergedRows = BuildSequence(length, start, other);
                mergedRows[mergedRows.Count - 1] = merged;
                return mergedRows;
            }

            return null;
        }

        private static int SumAlternating(int length, int start, int other)
        {
            if (length <= 0) return 0;
            int countStart = (length + 1) / 2;
            int countOther = length / 2;
            return countStart * start + countOther * other;
        }

        private static List<int> BuildSequence(int length, int start, int other)
        {
            var rows = new List<int>(length);
            for (int i = 0; i < length; i++)
            {
                rows.Add(i % 2 == 0 ? start : other);
            }

            return rows;
        }

        private static List<int> TryBuildDiamond(int total)
        {
            if (total < 3) return null;

            if (total % 3 == 2)
            {
                int outer = (total + 1) / 3;
                int center = outer - 1;
                if (outer >= 1 && center >= 1)
                {
                    return new List<int> { outer, center, outer };
                }
            }
            else if (total % 3 == 1)
            {
                int outer = (total - 1) / 3;
                int center = outer + 1;
                if (outer >= 1 && center >= 1)
                {
                    return new List<int> { outer, center, outer };
                }
            }
            else
            {
                int outer = total / 3;
                int center = outer + 1;
                if (outer >= 1 && center >= 1)
                {
                    return new List<int> { outer, center, outer };
                }
            }

            return null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnTransformChildrenChanged()
        {
            SetDirty();
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif
    }
}
