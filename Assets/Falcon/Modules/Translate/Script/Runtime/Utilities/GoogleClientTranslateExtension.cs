
/*
 * Author: VietHQ
 * Email: viethq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-07-24
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Falcon.Modules.Translate.Runtime
{
    public static class RectTransformExtension
    {
        /// <summary>
        ///  Assume targetRectTransform is the RectTransform you want to copy from and rectTransformTarget is the rectTransform you want to update
        /// </summary>
        /// <param name="rectTransform">This RectTransform component that want to be updated</param>
        /// <param name="targetRectTransform">Target RectTransform</param>
        public static void CopyFrom(this RectTransform rectTransform, RectTransform targetRectTransform)
        {
            // Assume rectTransformSource is the RectTransform you want to copy from

            // Copy the position and size of the Transform
            rectTransform.anchoredPosition = targetRectTransform.anchoredPosition;
            rectTransform.sizeDelta = targetRectTransform.sizeDelta;

            // Copy the pivot, anchorMin, and anchorMax
            rectTransform.pivot = targetRectTransform.pivot;
            rectTransform.anchorMin = targetRectTransform.anchorMin;
            rectTransform.anchorMax = targetRectTransform.anchorMax;

            // Copy the 3D position of the anchor
            rectTransform.anchoredPosition3D = targetRectTransform.anchoredPosition3D;

            // Copy the scale and rotation
            rectTransform.localScale = targetRectTransform.localScale;
            rectTransform.localRotation = targetRectTransform.localRotation;

            // Copy the edge offsets relative to the anchors
            rectTransform.offsetMin = targetRectTransform.offsetMin; // Left and bottom
            rectTransform.offsetMax = targetRectTransform.offsetMax; // Right and top
        }

        public static void ResizeToParentSize(this RectTransform rectTransform)
        {
            rectTransform.sizeDelta = Vector2.zero;

            rectTransform.pivot = Vector2.one * 0.5f;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.Euler(Vector3.zero);
            
            rectTransform.offsetMin = Vector2.zero; // Left and bottom
            rectTransform.offsetMax = Vector2.one; // Right and top
            
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localPosition = Vector3.zero;
        }
        
        public static Vector2 CalculateSize(this RectTransform childRectTransform, RectTransform parentRectTransform = null)
        {
            // Nếu không truyền parent thì mặc định lấy parent là parent của RectTransform hiện tại
            if (parentRectTransform == null) 
                parentRectTransform = (RectTransform) childRectTransform.parent;
            
            // Nếu parent vẫn null, báo lỗi và trả về default: Vector2.zero
            if (parentRectTransform == null)
            {
                Debug.LogError("Cannot calculate size if parent of RectTransform is null!");
                return Vector2.zero;
            }

            // Lấy kích thước của RectTransform cha
            Vector2 parentSize = parentRectTransform.rect.size;

            // Lấy thông tin về Anchors của RectTransform con
            Vector2 anchorMin = childRectTransform.anchorMin;
            Vector2 anchorMax = childRectTransform.anchorMax;

            // Tính toán kích thước dựa trên Anchors và kích thước cha
            Vector2 size = new Vector2(
                parentSize.x * (anchorMax.x - anchorMin.x),
                parentSize.y * (anchorMax.y - anchorMin.y)
            );

            // Thêm Size Delta vào kích thước nếu cần (Size Delta là sự chênh lệch so với kích thước được định nghĩa bởi anchors)
            size += childRectTransform.sizeDelta;

            return size;
        }
    }

    public static class TextExtension
    {
        /// <summary>
        /// Method to convert TextMeshPro alignment to Unity Text alignment
        /// </summary>
        /// <param name="text">This Text component</param>
        /// <param name="targetTextMeshProUGUI">Target TextMeshProUGUI Component</param>
        public static void ConvertAlignment(this Text text, TextMeshProUGUI targetTextMeshProUGUI)
        {
            TextAlignmentOptions tmProAlignment = targetTextMeshProUGUI.alignment;
            TextAnchor result = tmProAlignment switch
            {
                TextAlignmentOptions.TopLeft => TextAnchor.UpperLeft,
                TextAlignmentOptions.Top => TextAnchor.UpperCenter,
                TextAlignmentOptions.TopRight => TextAnchor.UpperRight,
                TextAlignmentOptions.Left => TextAnchor.MiddleLeft,
                TextAlignmentOptions.Center => TextAnchor.MiddleCenter,
                TextAlignmentOptions.Right => TextAnchor.MiddleRight,
                TextAlignmentOptions.BottomLeft => TextAnchor.LowerLeft,
                TextAlignmentOptions.Bottom => TextAnchor.LowerCenter,
                TextAlignmentOptions.BottomRight => TextAnchor.LowerRight,
                _ => TextAnchor.UpperLeft
            };

            text.alignment = result;
        }

        /// <summary>
        /// Get best fit font size depend on rectTransform size
        /// </summary>
        /// <param name="textComponent">This Text component</param>
        /// <param name="fitWidth">Fit by Width or not</param>
        /// <param name="fitHeight">Fit by Height or not</param>
        public static void SetGreatestFontSize(this Text textComponent, bool fitWidth = false, bool fitHeight = true)
        {
            int minSize = textComponent.resizeTextMinSize;
            int maxSize = textComponent.resizeTextMaxSize;
            int bestSize = minSize;

            var containerRectTransform = textComponent.rectTransform;
            
            // Test the font sizes from minSize to maxSize.
            for (int i = minSize; i <= maxSize; i++)
            {
                textComponent.fontSize = i;
                var sizeDelta = containerRectTransform.sizeDelta;
                bool fits = !(fitWidth && textComponent.preferredWidth > sizeDelta.x);

                if (fitHeight && textComponent.preferredHeight > containerRectTransform.sizeDelta.y)
                {
                    fits = false;
                }

                // If the text size is larger than the container size, stop the loop.
                if (fits)
                {
                    bestSize = i;
                }
                else
                {
                    break;
                }
            }
            // Apply the best found font size.
            textComponent.fontSize = bestSize;
        }
    }
}

