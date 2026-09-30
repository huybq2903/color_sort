/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-11
*/

namespace Falcon.Helpers.UI
{
    using System;
    using UnityEngine;

    [DefaultExecutionOrder(-999)]
    public class SafeArea : MonoBehaviour
    {
        RectTransform Panel;
        [SerializeField] bool ConformX = true;  // Conform to screen safe area on X-axis (default true, disable to ignore)
        [SerializeField] bool ConformY = true;  // Conform to screen safe area on Y-axis (default true, disable to ignore)
        [SerializeField] IgnoreArea ignore;
        [SerializeField] bool Logging = false;  // Enable logging for debugging

        private void Awake()
        {
            Panel = GetComponent<RectTransform>();

            if (Panel == null)
            {
                Debug.LogError("Cannot apply safe area - no RectTransform found on " + gameObject.name);
                return;
            }

            var safeArea = Screen.safeArea;
            if (safeArea != new Rect(0, 0, Screen.width, Screen.height))
            {
                ApplySafeArea(safeArea);
            }
        }

        private void ApplySafeArea(Rect r)
        {
            // Apply height offset percentage for IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                float ratioOffsetHeight = 0.0375f;
                float offsetHeight = r.height * ratioOffsetHeight;
                r.y -= offsetHeight / 2; // Adjust Y position to center the new height
                r.height += offsetHeight; // Adjust height by the offset percentage

                // Clamp the safe area to ensure it stays within screen bounds
                r.y = Mathf.Clamp(r.y, 0, Screen.height - r.height);
                r.height = Mathf.Clamp(r.height, 0, Screen.height);
            }

            // Ignore x-axis?
            if (!ConformX)
            {
                r.x = 0;
                r.width = Screen.width;
            }

            // Ignore y-axis?
            if (!ConformY)
            {
                r.y = 0;
                r.height = Screen.height;
            }

            if (Screen.width > 0 && Screen.height > 0)
            {
                // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
                Vector2 anchorMin = r.position;
                Vector2 anchorMax = r.position + r.size;
                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;

                if (ignore.HasFlag(IgnoreArea.Left))
                {
                    anchorMin.x = 0;
                }
                if (ignore.HasFlag(IgnoreArea.Bot))
                {
                    anchorMin.y = 0;
                }

                if (ignore.HasFlag(IgnoreArea.Right))
                {
                    anchorMax.x = 1;
                }
                if (ignore.HasFlag(IgnoreArea.Top))
                {
                    anchorMax.y = 1;
                }

                if (anchorMin.x >= 0 && anchorMin.y >= 0 && anchorMax.x >= 0 && anchorMax.y >= 0)
                {
                    Panel.anchorMin = anchorMin;
                    Panel.anchorMax = anchorMax;
                }
            }

            if (Logging)
            {
                Debug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}",
                    name, r.x, r.y, r.width, r.height, Screen.width, Screen.height);
            }
        }

        [Serializable]
        [Flags] // Cho phép sử dụng enum như một bitmask
        public enum IgnoreArea
        {
            None = 0, // Custom name for "Nothing" option
            Bot = 1 << 0,
            Top = 1 << 1,
            Left = 1 << 2,
            Right = 1 << 3,
            All = ~0, // Custom name for "Everything" option
        }
    }
}
