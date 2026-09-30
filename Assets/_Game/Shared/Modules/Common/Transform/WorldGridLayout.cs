/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-16
 */

using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Falcon.Shared.Common
{
    /// <summary>
    /// Sắp xếp các object con theo dạng lưới (giống GridLayoutGroup của UGUI)
    /// nhưng hoạt động với Transform 3D/2D trong World Space.
    /// </summary>
    [ExecuteAlways] // Cho phép script chạy trong cả Editor mode và Play mode
    public class WorldGridLayout : MonoBehaviour
    {
        public enum LayoutAxis
        {
            XY_2D, // Lưới đứng (trên mặt phẳng XY, như UI thông thường)
            XZ_3D  // Lưới nằm ngang (trên mặt đất XZ, phổ biến cho game 3D top-down)
        }

        public enum Corner
        {
            UpperLeft,
            UpperRight,
            LowerLeft,
            LowerRight
        }

        [Title("Settings")]
        [OnValueChanged("RepositionImmediate")] // Tự động gọi Reposition khi các giá trị này thay đổi trong Inspector
        public LayoutAxis axis = LayoutAxis.XZ_3D;

        [OnValueChanged("RepositionImmediate")]
        [Tooltip("Kích thước của mỗi ô trong lưới.")]
        public Vector2 cellSize = new Vector2(1f, 1f);

        [OnValueChanged("RepositionImmediate")]
        [Tooltip("Khoảng cách giữa các ô.")]
        public Vector2 spacing = Vector2.zero;

        [Tooltip("Số cột cố định. Để 0 hoặc âm nếu muốn tự động tính số cột dựa trên căn bậc hai của số lượng con.")]
        [OnValueChanged("RepositionImmediate")]
        [MinValue(0)]
        public int fixedColumnCount = 0;

        private readonly List<Transform> _activeChildrenBuffer = new();

        [Title("Alignment")]
        [OnValueChanged("RepositionImmediate")]
        [Tooltip("Góc bắt đầu sắp xếp các object con.")]
        public Corner startCorner = Corner.UpperLeft;
        
        [OnValueChanged("RepositionImmediate")]
        [Tooltip("Căn giữa toàn bộ lưới trong không gian của Parent.")]
        public bool centerParent = true;

        private void OnEnable()
        {
            // Đảm bảo layout được cập nhật khi component được bật hoặc khi thoát/vào Play mode
            RepositionImmediate();
        }

        private void LateUpdate()
        {
            // Trong Editor mode, kiểm tra nếu có sự thay đổi trên transform của parent hoặc con
            // và tự động gọi Reposition để cập nhật layout.
            // Sử dụng LateUpdate để đảm bảo tất cả các thay đổi khác đã được xử lý.
            if (!Application.isPlaying)
            {
                if (transform.hasChanged)
                {
                    RepositionImmediate();
                    // Đặt lại cờ hasChanged để tránh gọi Reposition không cần thiết.
                    // Lưu ý: Việc Reposition liên tục có thể ảnh hưởng đến hiệu suất trong editor nếu có nhiều object.
                    // transform.hasChanged = false; // Bỏ comment nếu muốn quản lý hasChanged thủ công
                }
            }
        }
        public void RepositionImmediate() => Reposition(0f);

        public WorldGridLayout SetColumnCount(int count)
        {
            fixedColumnCount = count;
            return this;
        }
        
        // Nút bấm trong Inspector để cập nhật layout thủ công
        [Button("Update Layout", ButtonSizes.Large)]
        public void Reposition(float duration)
        {
            _activeChildrenBuffer.Clear();
            var activeChildren = _activeChildrenBuffer;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.gameObject.activeInHierarchy)
                {
                    activeChildren.Add(child);
                }
            }

            int childCount = activeChildren.Count;
            if (childCount == 0) return;

            int columns = fixedColumnCount;
            if (columns <= 0)
            {
                // Nếu không có số cột cố định, tính toán để tạo một hình vuông gần đúng
                columns = Mathf.CeilToInt(Mathf.Sqrt(childCount));
                if (columns == 0) columns = 1; // Đảm bảo luôn có ít nhất 1 cột
            }
            
            int rows = Mathf.CeilToInt((float)childCount / columns);

            // Tính toán tổng kích thước của lưới
            float totalWidth = (columns * cellSize.x) + ((columns - 1) * spacing.x);
            float totalHeight = (rows * cellSize.y) + ((rows - 1) * spacing.y);

            Vector3 startOffset = Vector3.zero;

            // Tính toán offset để căn giữa toàn bộ lưới nếu centerParent là true
            if (centerParent)
            {
                startOffset.x = -totalWidth / 2f + cellSize.x / 2f;
                // Đối với trục Y hoặc Z, tùy thuộc vào LayoutAxis
                float verticalOffset = totalHeight / 2f - cellSize.y / 2f;
                if (axis == LayoutAxis.XY_2D) startOffset.y = verticalOffset;
                else startOffset.z = verticalOffset;
            }

            for (int i = 0; i < childCount; i++)
            {
                Transform child = activeChildren[i];
                
                int row = i / columns; // Hàng hiện tại
                int col = i % columns; // Cột hiện tại

                // Tính toán vị trí tương đối của object con
                float xPos = col * (cellSize.x + spacing.x);
                float yPos = row * (cellSize.y + spacing.y);

                Vector3 targetLocalPos = Vector3.zero;

                // Áp dụng offset ban đầu và vị trí tính toán
                if (axis == LayoutAxis.XY_2D)
                {
                    targetLocalPos.x = startOffset.x + xPos;
                    // Sắp xếp từ trên xuống (giá trị Y giảm dần) nếu là UpperLeft/UpperRight
                    targetLocalPos.y = startOffset.y - yPos; 
                    targetLocalPos.z = 0; // Trục Z không thay đổi
                }
                else // XZ_3D
                {
                    targetLocalPos.x = startOffset.x + xPos;
                    targetLocalPos.y = 0; // Trục Y không thay đổi
                    // Sắp xếp từ trên xuống (giá trị Z giảm dần) nếu là UpperLeft/UpperRight (trên mặt phẳng XZ)
                    targetLocalPos.z = startOffset.y - yPos; 
                }

                // Điều chỉnh vị trí dựa trên startCorner (hiện tại code đang fix cứng UpperLeft)
                // Cần thêm logic điều chỉnh nếu startCorner không phải UpperLeft
                switch (startCorner)
                {
                    case Corner.UpperLeft:
                        // Đã xử lý mặc định
                        break;
                    case Corner.UpperRight:
                        targetLocalPos.x = startOffset.x + totalWidth - (xPos + cellSize.x);
                        break;
                    case Corner.LowerLeft:
                        if (axis == LayoutAxis.XY_2D) targetLocalPos.y = startOffset.y + totalHeight - (yPos + cellSize.y);
                        else targetLocalPos.z = startOffset.y + totalHeight - (yPos + cellSize.y);
                        break;
                    case Corner.LowerRight:
                        targetLocalPos.x = startOffset.x + totalWidth - (xPos + cellSize.x);
                        if (axis == LayoutAxis.XY_2D) targetLocalPos.y = startOffset.y + totalHeight - (yPos + cellSize.y);
                        else targetLocalPos.z = startOffset.y + totalHeight - (yPos + cellSize.y);
                        break;
                }

                if (duration == 0)
                {
                    child.localPosition = targetLocalPos;
                }
                else
                {
                    child.DOLocalMove(targetLocalPos, duration);
                }
            }
        }
    }
}
