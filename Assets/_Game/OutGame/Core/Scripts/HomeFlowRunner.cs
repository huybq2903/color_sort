/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-08-07
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Modules.Core.UI.Runtime;
using Falcon.Shared.Common;
using UnityEngine;

namespace Falcon.OutGame.Core
{
    /// <summary>Chạy FlowSequence popup mỗi lần vào Home. Module ngoài tự Add step vào GameKeys.HOME_FLOW.</summary>
    public static class HomeFlowRunner
    {
        public const int ORDER_PROGRESS = 0;
        public const int ORDER_RESOURCE_CLAIM = 100;
        public const int ORDER_PROMOTE = 200;

        /// <summary>Delay trước khi chạy step đầu, chờ Home vào xong.</summary>
        public static float delayToHome = 1f;

        // Popup load qua Addressables nên phải chờ; timeout để 1 popup lỗi không treo cả flow.
        private const float TIMEOUT_OPEN = 5f;

        private static bool _registered;

        private static FlowSequence Sequence => Flow.Of(GameKeys.HOME_FLOW);

        /// <summary>Gọi ở Start của màn Home.</summary>
        public static async UniTaskVoid Run()
        {
            RegisterBuiltInSteps();
            await Sequence.Run();
        }

        /// <summary>Gọi ở OnDisable của màn Home.</summary>
        public static void Stop() => Sequence.Stop();

        /// <summary>Mở popup rồi chờ đóng. popupClose khác popupOpen khi cần chờ popup khác đóng mới đi tiếp.</summary>
        public static async UniTask OpenPopupAwaitClose(string popupOpen, string popupClose = null, CancellationToken ct = default)
        {
            UIWrapper.OpenPopup(popupOpen);
            await AwaitPopupClosed(popupClose ?? popupOpen, ct);
        }

        /// <summary>Chờ popup xuất hiện rồi biến mất khỏi stack.</summary>
        public static async UniTask AwaitPopupClosed(string popupName, CancellationToken ct = default)
        {
            var deadline = Time.realtimeSinceStartup + TIMEOUT_OPEN;
            await UniTask.WaitUntil(
                () => UIWrapper.IsPopupStackActiveInHierarchy(popupName) || Time.realtimeSinceStartup > deadline,
                cancellationToken: ct);

            if (!UIWrapper.IsPopupStackActiveInHierarchy(popupName))
            {
                Debug.LogWarning($"[HomeFlow] popup '{popupName}' không mở được sau {TIMEOUT_OPEN}s, bỏ qua step.");
                return;
            }

            await UniTask.WaitWhile(() => UIWrapper.IsPopupStackActiveInHierarchy(popupName), cancellationToken: ct);
        }

        private static void RegisterBuiltInSteps()
        {
            if (_registered) return;
            _registered = true;

            Sequence.Add(DelayToHome, int.MinValue);
        }

        private static UniTask DelayToHome(CancellationToken ct)
            => UniTask.Delay(TimeSpan.FromSeconds(delayToHome), cancellationToken: ct);
    }
}
