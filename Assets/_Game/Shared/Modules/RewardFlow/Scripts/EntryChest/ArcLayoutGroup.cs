/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-10-16
 */

namespace Falcon.OutGame.UIRewardClaim
{
    using UnityEngine;
    using UnityEngine.UI;

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class ArcLayoutGroup : LayoutGroup
    {
        [Range(0, 360)] public float sweepAngle = 140f;
        public float baseRadius = 200f; // bán kính của hàng trung tâm
        public float rowSpacing = 120f; // khoảng cách giữa các hàng
        public int maxPerRow = 6;

        public override void CalculateLayoutInputVertical() { }

        public override void SetLayoutHorizontal() => Arrange();
        public override void SetLayoutVertical() => Arrange();

        void Arrange()
        {
            int total = rectChildren.Count;
            if (total == 0) return;

            // Tính số hàng
            int rows = Mathf.CeilToInt((float)total / maxPerRow);

            int idx = 0;

            // Nếu có nhiều hàng → ta cần tính offset hàng để đối xứng quanh trục giữa
            for (int row = 0; row < rows; row++)
            {
                int count = Mathf.Min(maxPerRow, total - idx);
                if (count <= 0) break;

                // 🧮 Tính offset hàng để cân quanh trục giữa
                float centerOffset = GetRowVerticalOffset(row, rows);
                float radius = baseRadius + centerOffset;

                float minAngle = 30f; // cung tối thiểu khi ít item
                float maxAngle = sweepAngle;

                float t = Mathf.InverseLerp(1, maxPerRow, count); // 1→maxPerRow
                float actualSweep = Mathf.Lerp(minAngle, maxAngle, t);

                float step = (count > 1 ? actualSweep / (count - 1) : 0f);
                float start = -step * (count - 1) * 0.5f;

                for (int i = 0; i < count; i++)
                {
                    float ang = start + i * step;
                    float rad = ang * Mathf.Deg2Rad;
                    Vector2 pos = new Vector2(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius);

                    RectTransform c = rectChildren[idx++];
                    c.localPosition = pos;
                    c.localRotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// Tính khoảng cách hàng so với tâm (0 = giữa).
        /// Các hàng phân bố đối xứng trên – dưới.
        /// </summary>
        float GetRowVerticalOffset(int row, int totalRows)
        {
            if (totalRows == 1) return 0f;

            // ví dụ totalRows = 3 → offset = [ +1, 0, -1 ]
            // totalRows = 4 → [ +1.5, +0.5, -0.5, -1.5 ]
            float half = (totalRows - 1) * 0.5f;
            float relative = half - row; // 0 ở giữa
            return relative * rowSpacing;
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