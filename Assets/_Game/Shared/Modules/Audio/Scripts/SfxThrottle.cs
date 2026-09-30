/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-15
 */

using System.Collections.Generic;
using UnityEngine;

namespace Falcon.Shared.Audio
{
    // Chặn spam cùng một tiếng: quá gần lần trước, hoặc đang có quá nhiều voice của chính nó
    internal class SfxThrottle
    {
        private const float MinInterval = 0.02f;
        private const int MaxVoicesPerClip = 4;

        private readonly Dictionary<string, int> _activeCounts = new();
        private readonly Dictionary<string, float> _lastPlayTime = new();

        public bool TryReserve(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return false;
            }

            var now = Time.unscaledTime;
            if (_lastPlayTime.TryGetValue(clipName, out var lastTime) && now - lastTime < MinInterval)
            {
                return false;
            }

            _activeCounts.TryGetValue(clipName, out var count);
            if (count >= MaxVoicesPerClip)
            {
                return false;
            }

            _lastPlayTime[clipName] = now;
            _activeCounts[clipName] = count + 1;
            return true;
        }

        public void Release(string clipName)
        {
            if (string.IsNullOrEmpty(clipName) || !_activeCounts.TryGetValue(clipName, out var count))
            {
                return;
            }

            count = Mathf.Max(0, count - 1);
            if (count == 0)
            {
                _activeCounts.Remove(clipName);
            }
            else
            {
                _activeCounts[clipName] = count;
            }
        }
    }
}
