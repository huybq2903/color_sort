/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-10
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Falcon.Modules.Packs.Core.Runtime
{
    /// <summary>
    /// Lớp tiện ích đảm bảo rằng một hành động lặp lại (với cùng một key)
    /// chỉ được thực thi một lần sau một loạt các lần gọi nhanh liên tiếp.
    /// 
    /// Hữu ích cho các sự kiện lặp lại—như cập nhật UI, tải dữ liệu,
    /// hoặc gửi request mạng—giúp công việc thực sự chỉ được thực hiện một lần sau một đợt kích hoạt.
    /// </summary>
    public static class DoAllTheSameThingOneTime
    {
        private static readonly Dictionary<string, DoAllTheSameThing> _dictionary = new();

        /// <summary>
        /// Thực thi một hành động sau một khoảng trễ xác định, đảm bảo rằng với cùng một key,
        /// chỉ lần kích hoạt cuối cùng mới thực sự thực hiện hành động.
        /// Ngoài ra, có thể chạy một hành động khác ngay lập tức ở mỗi lần gọi.
        /// </summary>
        /// <param name="key">Định danh duy nhất cho nhóm hành động.</param>
        /// <param name="actionDoManyTime">Hành động thực thi ngay lập tức (mỗi lần gọi), có thể null.</param>
        /// <param name="actionSameDoOneTime">Hành động chỉ thực thi một lần sau khoảng trễ (chỉ giữ lại lần cuối), có thể null.</param>
        /// <param name="delayMs">Thời gian trễ (ms) trước khi thực thi hành động debounce. Mặc định là 100 ms.</param>
        public static void DoAction(string key, Action actionDoManyTime, Action actionSameDoOneTime, int delayMs = 100)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary.Add(key, new DoAllTheSameThing());
            }
            _dictionary[key].DoAction(actionDoManyTime, actionSameDoOneTime, delayMs);
        }

        private class DoAllTheSameThing
        {
            private Action _actionMain;
            private CancellationTokenSource _cts;

            public void DoAction(Action actionDoManyTime, Action actionSameDoOneTime, int delayMs)
            {
                actionDoManyTime?.Invoke();

                _actionMain = actionSameDoOneTime;

                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }

                _cts = new CancellationTokenSource();

                _ = DoDelayAsync(delayMs, _cts.Token);
            }

            private async Task DoDelayAsync(int delayMs, CancellationToken token)
            {
                try
                {
                    await Task.Delay(delayMs, token);
                    _actionMain?.Invoke();
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
    }
}